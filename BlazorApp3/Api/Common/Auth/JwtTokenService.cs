using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BlazorApp3.Data;
using BlazorApp3.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BlazorApp3.Api.Auth;

public sealed record TokenResponse(
    string AccessToken, DateTime AccessTokenExpiresAt,
    string RefreshToken, DateTime RefreshTokenExpiresAt,
    string TokenType = "Bearer");

/// <summary>
/// Emite access tokens (JWT curto) e refresh tokens (opacos, "{sessionId}.{segredo}").
/// Cada login pela API é uma sessão em UserSessions (aparece na tela de segurança e pode ser encerrada de lá).
/// O refresh token é rotacionado a cada uso; reutilizar um já trocado encerra a sessão (sinal de vazamento).
/// </summary>
public sealed class JwtTokenService(
    IOptions<JwtOptions> options,
    SignInManager<AppUser> signInManager,
    UserSessions sessions,
    AppDbContext db,
    IOptions<IdentityOptions> identityOptions)
{
    private readonly JwtOptions _jwt = options.Value;
    private static readonly JsonWebTokenHandler Handler = new();

    /// <summary>Cria a sessão da API e devolve o par de tokens.</summary>
    public async Task<TokenResponse> IssueAsync(HttpContext http, AppUser user, bool mfa)
    {
        var (secret, hash) = NewSecret();
        var refreshExpires = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays);
        var session = await sessions.CreateSessionAsync(http, signInManager.UserManager, user, persistent: true, mfa, hash, refreshExpires);

        var response = await BuildAsync(user, session.Id, mfa, secret, refreshExpires);
        await sessions.ApplySessionPolicyAsync(session); // sessão única / aviso de novo acesso
        return response;
    }

    /// <summary>Troca o refresh token por um par novo. Null = token inválido, expirado, reutilizado ou sessão encerrada.</summary>
    public async Task<TokenResponse?> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var dot = refreshToken.IndexOf('.');
        if (dot <= 0 || dot == refreshToken.Length - 1) return null;
        var sid = refreshToken[..dot];
        var presentedHash = Hash(refreshToken[(dot + 1)..]);

        var session = await db.UserSessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sid, ct);
        if (session is not { RevokedAt: null, RefreshTokenHash: not null }) return null;

        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(presentedHash), Encoding.ASCII.GetBytes(session.RefreshTokenHash)))
        {
            // Token antigo (já rotacionado) sendo reapresentado: alguém pode ter copiado. Encerra a sessão.
            await sessions.RevokeAsync(sid, SessionRevokeReason.Security);
            return null;
        }
        if (session.RefreshTokenExpiresAt is not { } exp || exp <= DateTime.UtcNow)
        {
            await sessions.RevokeAsync(sid, SessionRevokeReason.Logout);
            return null;
        }

        var users = signInManager.UserManager;
        var user = await users.FindByIdAsync(session.UserId);
        if (user is null || !user.IsActive || await users.IsLockedOutAsync(user))
        {
            await sessions.RevokeAsync(sid, SessionRevokeReason.Blocked);
            return null;
        }

        // Rotação atômica: só troca se o hash ainda for o apresentado (duas renovações simultâneas → só uma vence)
        var (secret, hash) = NewSecret();
        var now = DateTime.UtcNow;
        var refreshExpires = now.AddDays(_jwt.RefreshTokenDays);
        var oldHash = session.RefreshTokenHash;
        var changed = await db.UserSessions
            .Where(s => s.Id == sid && s.RevokedAt == null && s.RefreshTokenHash == oldHash)
            .ExecuteUpdateAsync(u => u
                .SetProperty(s => s.RefreshTokenHash, hash)
                .SetProperty(s => s.RefreshTokenExpiresAt, refreshExpires)
                .SetProperty(s => s.LastSeenAt, now), ct);
        if (changed == 0) return null;

        return await BuildAsync(user, sid, session.Mfa, secret, refreshExpires);
    }

    private async Task<TokenResponse> BuildAsync(AppUser user, string sessionId, bool mfa, string secret, DateTime refreshExpires)
    {
        // Mesmos claims do cookie (factory do Identity), sem o security stamp
        var principal = await signInManager.CreateUserPrincipalAsync(user);
        var stampType = identityOptions.Value.ClaimsIdentity.SecurityStampClaimType;
        var claims = principal.Claims.Where(c => c.Type != stampType).ToList();
        claims.Add(new(JwtRegisteredClaimNames.Sub, user.Id));
        claims.Add(new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")));
        claims.Add(new(UserSessions.ClaimType, sessionId));
        if (mfa) claims.Add(new(UserSessions.AmrClaimType, "mfa"));

        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
        var token = Handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Expires = expires,
            SigningCredentials = new SigningCredentials(_jwt.Key(), SecurityAlgorithms.HmacSha256)
        });

        return new TokenResponse(token, expires, $"{sessionId}.{secret}", refreshExpires);
    }

    private static (string Secret, string Hash) NewSecret()
    {
        var secret = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        return (secret, Hash(secret));
    }

    private static string Hash(string secret) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
}
