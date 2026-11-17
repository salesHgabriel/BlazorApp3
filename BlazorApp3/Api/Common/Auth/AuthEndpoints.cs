using System.Security.Claims;
using BlazorApp3.Data;
using BlazorApp3.Features.Account;
using BlazorApp3.Features.Notifications;
using BlazorApp3.Features.Settings;
using BlazorApp3.Localization;
using BlazorApp3.Security;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp3.Api.Auth;

/// <summary>
/// /api/auth — login da API com as mesmas regras da tela de login (inativo, bloqueio, e-mail não confirmado, MFA).
/// Erros vêm como ProblemDetails com a extensão "code" para o cliente decidir o que fazer.
/// </summary>
public static class AuthEndpoints
{
    /// <param name="Code">Código do app autenticador (ou de recuperação) quando a conta tem MFA.</param>
    public sealed record TokenRequest(string Email, string Password, string? Code);
    public sealed record RefreshRequest(string RefreshToken);

    public static void MapAuthEndpoints(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/auth").WithTags("Auth");

        g.MapPost("/token", LoginAsync).AllowAnonymous();

        g.MapPost("/refresh", async (RefreshRequest r, JwtTokenService tokens, CancellationToken ct) =>
            string.IsNullOrWhiteSpace(r.RefreshToken) || await tokens.RefreshAsync(r.RefreshToken.Trim(), ct) is not { } response
                ? Error(StatusCodes.Status401Unauthorized, "invalid_refresh_token", AppText.Get("Sessão expirada. Entre novamente."))
                : TypedResults.Ok(response))
            .AllowAnonymous();

        // Encerra a sessão do token (o access token deixa de valer na hora e o refresh token também)
        g.MapPost("/logout", async (ClaimsPrincipal me, UserSessions sessions) =>
        {
            if (me.FindFirstValue(UserSessions.ClaimType) is { } sid)
                await sessions.RevokeAsync(sid, SessionRevokeReason.Logout);
            return TypedResults.NoContent();
        });
    }

    private static async Task<IResult> LoginAsync(TokenRequest r, HttpContext http, UserManager<AppUser> users,
        SignInManager<AppUser> signInManager, SystemSettingsProvider settingsProvider, INotifier notifier, JwtTokenService tokens)
    {
        var invalid = Error(StatusCodes.Status401Unauthorized, "invalid_credentials", AppText.Get("E-mail ou senha inválidos."));
        var tooManyAttempts = Error(StatusCodes.Status403Forbidden, "account_locked",
            AppText.Get("Conta bloqueada temporariamente por excesso de tentativas. Tente novamente em alguns minutos."));

        if (string.IsNullOrWhiteSpace(r.Email) || string.IsNullOrEmpty(r.Password)) return invalid;
        var user = await users.FindByEmailAsync(r.Email.Trim());
        if (user is null) return invalid;

        if (!user.IsActive)
            return Error(StatusCodes.Status403Forbidden, "account_inactive", AppText.Get("Sua conta está inativa. Contate o administrador."));

        // Bloqueada: o motivo do bloqueio administrativo só aparece com a senha correta
        if (await users.IsLockedOutAsync(user))
        {
            if (!BlockState.IsAdminBlock(user) || !await users.CheckPasswordAsync(user, r.Password)) return tooManyAttempts;
            var message = BlockState.IsPermanent(user.LockoutEnd)
                ? AppText.Get("Sua conta foi bloqueada por um administrador.")
                : AppText.Format("Sua conta está bloqueada até {0}.", user.LockoutEnd!.Value.ToLocalTime().ToString("g"));
            if (!string.IsNullOrWhiteSpace(user.BlockReason)) message += " " + AppText.Format("Motivo: {0}", user.BlockReason);
            return Error(StatusCodes.Status403Forbidden, "account_blocked", message);
        }

        var settings = await settingsProvider.GetAsync();
        if (settings.RequireEmailConfirmation && !user.EmailConfirmed)
        {
            if (await users.CheckPasswordAsync(user, r.Password))
                return Error(StatusCodes.Status403Forbidden, "email_not_confirmed", AppText.Get("Confirme seu e-mail antes de entrar."));
            await users.AccessFailedAsync(user);
            return invalid;
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, r.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            notifier.LockoutAlert(user.Id);
            return tooManyAttempts;
        }
        if (!result.Succeeded) return invalid;

        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(r.Code))
                return Error(StatusCodes.Status401Unauthorized, "mfa_required", AppText.Get("Informe o código do app autenticador."));

            if (!await ReauthVerifier.VerifyCodeAsync(users, user, r.Code.Trim()))
            {
                await users.AccessFailedAsync(user);
                if (await users.IsLockedOutAsync(user))
                {
                    notifier.LockoutAlert(user.Id);
                    return tooManyAttempts;
                }
                return Error(StatusCodes.Status401Unauthorized, "invalid_mfa_code", AppText.Get("Código de verificação inválido."));
            }
            await users.ResetAccessFailedCountAsync(user);
        }

        return TypedResults.Ok(await tokens.IssueAsync(http, user, mfa: user.TwoFactorEnabled));
    }

    private static IResult Error(int status, string code, string detail) =>
        TypedResults.Problem(detail, statusCode: status, title: code, extensions: new Dictionary<string, object?> { ["code"] = code });
}
