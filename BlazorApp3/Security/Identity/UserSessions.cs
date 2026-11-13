using System.Security.Claims;
using BlazorApp3.Data;
using BlazorApp3.Features.Account;
using BlazorApp3.Features.Notifications;
using BlazorApp3.Features.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BlazorApp3.Security;

/// <summary>
/// Sessões de login (tabela UserSessions, uma por navegador/dispositivo). Todo login cria uma sessão e grava o Id
/// no claim <c>session_id</c> do cookie. Cada navegador conectado entra no grupo SignalR da sua sessão.
/// <list type="bullet">
/// <item>Sessão única ligada: o login novo encerra as outras sessões (evento <c>sessionRevoked</c>).</item>
/// <item>Sessão única desligada: as outras sessões recebem <c>newSession</c> com o dispositivo e podem encerrar a nova
/// (pedindo senha e, se configurado, o código MFA).</item>
/// <item>Sessão encerrada: o cookie é recusado na próxima requisição (<see cref="ValidatePrincipalAsync"/>) e o circuito
/// Blazor cai na revalidação periódica.</item>
/// </list>
/// </summary>
public sealed class UserSessions(
    IServiceScopeFactory scopeFactory,
    IMemoryCache cache,
    SystemSettingsProvider settings,
    IHubContext<NotificationsHub> hub,
    ILogger<UserSessions> logger)
{
    public const string ClaimType = "session_id";
    /// <summary>Claim "amr" = "mfa" quando o login passou pelo segundo fator.</summary>
    public const string AmrClaimType = "amr";
    private const string RememberClaimType = "remember";

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan TouchInterval = TimeSpan.FromMinutes(1);

    private sealed record CachedSession(string UserId, bool Active);

    public static string Group(string sessionId) => "session:" + sessionId;

    // ------------------------------------------------------------------ Login

    /// <summary>Cria a sessão, faz o login (cookie com session_id) e aplica sessão única / aviso de novo acesso.</summary>
    public async Task<string> SignInAsync(SignInManager<AppUser> signInManager, AppUser user, bool persistent, bool mfa = false)
    {
        var http = signInManager.Context;
        var ua = http.Request.Headers.UserAgent.ToString();
        var device = DeviceInfo.Parse(ua);
        var now = DateTime.UtcNow;
        var session = new UserSession
        {
            Id = Guid.NewGuid().ToString("N"), UserId = user.Id, CreatedAt = now, LastSeenAt = now,
            Persistent = persistent, Mfa = mfa,
            IpAddress = http.Connection.RemoteIpAddress?.ToString(),
            UserAgent = ua.Length > 512 ? ua[..512] : ua,
            Browser = device.Browser, Os = device.Os, DeviceType = device.DeviceType
        };

        user.LastLoginAt = now;
        var result = await signInManager.UserManager.UpdateAsync(user);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.UserSessions.Add(session);
            await db.SaveChangesAsync();
        }
        cache.Set(Key(session.Id), new CachedSession(user.Id, true), CacheTtl);

        List<Claim> claims = [new(ClaimType, session.Id)];
        if (mfa) claims.Add(new(AmrClaimType, "mfa"));
        await signInManager.SignInWithClaimsAsync(user, persistent, claims);

        var s = await settings.GetAsync();
        if (s.SingleSessionEnabled)
            await RevokeAllAsync(user.Id, SessionRevokeReason.Replaced, exceptSessionId: session.Id);
        else if (s.NewLoginAlertsEnabled)
            await AlertOtherSessionsAsync(session);

        return session.Id;
    }

    /// <summary>Avisa as outras sessões abertas (tempo real) e registra uma notificação de segurança.</summary>
    private async Task AlertOtherSessionsAsync(UserSession session)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var others = await db.UserSessions
            .Where(x => x.UserId == session.UserId && x.Id != session.Id && x.RevokedAt == null)
            .Select(x => x.Id).ToListAsync();
        if (others.Count == 0) return;

        var info = SessionInfo.From(session);
        try { await hub.Clients.Groups(others.Select(Group).ToList()).SendAsync(NotificationsHub.NewSessionEvent, info); }
        catch (Exception ex) { logger.LogWarning(ex, "Falha ao avisar as sessões de {UserId} sobre novo acesso.", session.UserId); }

        scope.ServiceProvider.GetRequiredService<INotifier>().NewLogin(session);
    }

    // ------------------------------------------------------------------ Validação

    /// <summary>A sessão do principal existe e não foi encerrada?</summary>
    public async Task<bool> IsCurrentAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return true; // anônimo: nada a validar
        var sid = principal.FindFirstValue(ClaimType);
        if (sid is null) return false;   // cookie antigo, sem sessão
        var entry = await GetAsync(sid, ct);
        return entry is { Active: true } && entry.UserId == userId;
    }

    private async Task<CachedSession?> GetAsync(string sid, CancellationToken ct)
    {
        if (cache.TryGetValue(Key(sid), out CachedSession? entry)) return entry;
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        entry = await db.UserSessions.Where(s => s.Id == sid)
            .Select(s => new CachedSession(s.UserId, s.RevokedAt == null)).FirstOrDefaultAsync(ct);
        cache.Set(Key(sid), entry, CacheTtl);
        return entry;
    }

    /// <summary>Atualiza "visto por último" (no máximo 1 gravação por minuto por sessão).</summary>
    public async Task TouchAsync(string? sid, CancellationToken ct = default)
    {
        if (sid is null || cache.TryGetValue(TouchKey(sid), out _)) return;
        cache.Set(TouchKey(sid), true, TouchInterval);
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        await db.UserSessions.Where(s => s.Id == sid && s.RevokedAt == null)
            .ExecuteUpdateAsync(u => u.SetProperty(s => s.LastSeenAt, now), ct);
    }

    // ------------------------------------------------------------------ Encerramento

    /// <summary>Encerra uma sessão: grava no banco, invalida o cache e derruba os navegadores dela (SignalR).</summary>
    public async Task<bool> RevokeAsync(string sessionId, string reason, string? bySessionId = null)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var changed = await db.UserSessions.Where(s => s.Id == sessionId && s.RevokedAt == null)
            .ExecuteUpdateAsync(u => u
                .SetProperty(s => s.RevokedAt, now)
                .SetProperty(s => s.RevokedReason, reason)
                .SetProperty(s => s.RevokedBySessionId, bySessionId));
        if (changed == 0) return false;
        await KickAsync([sessionId], reason);
        return true;
    }

    /// <summary>Encerra todas as sessões do usuário (opcionalmente mantendo uma). Retorna quantas foram encerradas.</summary>
    public async Task<int> RevokeAllAsync(string userId, string reason, string? exceptSessionId = null, string? bySessionId = null)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ids = await db.UserSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null && s.Id != exceptSessionId)
            .Select(s => s.Id).ToListAsync();
        if (ids.Count == 0) return 0;

        var now = DateTime.UtcNow;
        await db.UserSessions.Where(s => ids.Contains(s.Id))
            .ExecuteUpdateAsync(u => u
                .SetProperty(s => s.RevokedAt, now)
                .SetProperty(s => s.RevokedReason, reason)
                .SetProperty(s => s.RevokedBySessionId, bySessionId));
        await KickAsync(ids, reason);
        return ids.Count;
    }

    private async Task KickAsync(IReadOnlyList<string> sessionIds, string reason)
    {
        foreach (var id in sessionIds)
        {
            if (cache.TryGetValue(Key(id), out CachedSession? c) && c is not null) cache.Set(Key(id), c with { Active = false }, CacheTtl);
            else cache.Remove(Key(id));
        }
        try { await hub.Clients.Groups(sessionIds.Select(Group).ToList()).SendAsync(NotificationsHub.SessionRevokedEvent, reason); }
        catch (Exception ex) { logger.LogWarning(ex, "Falha ao avisar sessões encerradas."); }
    }

    private static string Key(string sid) => "session:" + sid;
    private static string TouchKey(string sid) => "session-touch:" + sid;

    // ------------------------------------------------------------------ Cookie

    /// <summary>Validação do cookie: mantém a do security stamp do Identity e acrescenta a da sessão.</summary>
    public static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
    {
        await SecurityStampValidator.ValidatePrincipalAsync(context);
        if (context.Principal?.Identity?.IsAuthenticated != true) return;

        var sessions = context.HttpContext.RequestServices.GetRequiredService<UserSessions>();
        if (!await sessions.IsCurrentAsync(context.Principal))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }
        await sessions.TouchAsync(context.Principal.FindFirstValue(ClaimType));
    }

    /// <summary>
    /// O security stamp validator recria o principal pela factory (sem os claims extras do login).
    /// Copia o session_id e o amr do principal antigo para não derrubar a sessão a cada revalidação.
    /// </summary>
    public static Task CarryOverClaimsAsync(SecurityStampRefreshingPrincipalContext context)
    {
        if (context.NewPrincipal?.Identity is not ClaimsIdentity identity || context.CurrentPrincipal is null) return Task.CompletedTask;
        foreach (var type in new[] { ClaimType, AmrClaimType })
        {
            var claim = context.CurrentPrincipal.FindFirst(type);
            if (claim is not null && !identity.HasClaim(c => c.Type == type)) identity.AddClaim(new Claim(type, claim.Value));
        }
        return Task.CompletedTask;
    }

    // ------------------------------------------------------------------ Login em 2 etapas (MFA)

    /// <summary>Cookie temporário do Identity (TwoFactorUserId) entre a senha e o código MFA.</summary>
    public static Task BeginTwoFactorAsync(HttpContext http, string userId, bool remember)
    {
        var identity = new ClaimsIdentity(IdentityConstants.TwoFactorUserIdScheme);
        identity.AddClaim(new Claim(ClaimTypes.Name, userId)); // lido por TwoFactorUserIdAsync
        identity.AddClaim(new Claim(RememberClaimType, remember ? "1" : "0"));
        return http.SignInAsync(IdentityConstants.TwoFactorUserIdScheme, new ClaimsPrincipal(identity));
    }

    /// <summary>Id do usuário que passou pela senha e aguarda o código MFA (null = expirou ou não existe).</summary>
    public static async Task<string?> TwoFactorUserIdAsync(HttpContext http)
    {
        var result = await http.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme);
        return result.Succeeded ? result.Principal?.FindFirstValue(ClaimTypes.Name) : null;
    }

    public static async Task<bool> TwoFactorRememberAsync(HttpContext http)
    {
        var result = await http.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme);
        return result.Principal?.FindFirstValue(RememberClaimType) == "1";
    }

    public static Task EndTwoFactorAsync(HttpContext http) => http.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
}

/// <summary>Dados de uma sessão enviados ao navegador (aviso de novo acesso) e exibidos na tela de segurança.</summary>
public sealed record SessionInfo(string Id, string Browser, string Os, string DeviceType, string? IpAddress, DateTime CreatedAt)
{
    public static SessionInfo From(UserSession s) => new(s.Id, s.Browser, s.Os, s.DeviceType, s.IpAddress, s.CreatedAt);
}
