using BlazorApp3.Data;
using BlazorApp3.Localization;
using BlazorApp3.Services;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp3.Features.Account;

/// <summary>
/// Confirmação de identidade antes de ações sensíveis (encerrar sessões, desativar MFA...):
/// senha + código do app autenticador (ou código de recuperação) quando o MFA está ativo.
/// Erros contam como tentativa falha (lockout do Identity).
/// </summary>
public sealed class ReauthVerifier(UserManager<AppUser> users)
{
    public async Task<AppUser> VerifyAsync(string userId, string? password, string? code)
    {
        var user = await users.FindByIdAsync(userId) ?? throw new AppException(AppText.Get("Usuário não encontrado."));
        if (await users.IsLockedOutAsync(user))
            throw new AppException(AppText.Get("Conta bloqueada temporariamente por excesso de tentativas. Tente novamente em alguns minutos."));

        if (string.IsNullOrEmpty(password) || !await users.CheckPasswordAsync(user, password))
        {
            await users.AccessFailedAsync(user);
            throw new AppException(AppText.Get("Senha incorreta."));
        }

        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new AppException(AppText.Get("Informe o código do app autenticador."));
            if (!await VerifyCodeAsync(users, user, code))
            {
                await users.AccessFailedAsync(user);
                throw new AppException(AppText.Get("Código de verificação inválido."));
            }
        }

        await users.ResetAccessFailedCountAsync(user);
        return user;
    }

    /// <summary>6 dígitos = código TOTP do app; outro formato = código de recuperação (consumido).</summary>
    public static async Task<bool> VerifyCodeAsync(UserManager<AppUser> users, AppUser user, string code)
    {
        var clean = code.Replace(" ", "").Replace("-", "").Trim();
        if (clean.Length == 6 && clean.All(char.IsDigit))
            return await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, clean);
        // Códigos de recuperação do Identity: XXXXX-XXXXX (aceita sem hífen e em minúsculas)
        var recovery = clean.ToUpperInvariant();
        if (recovery.Length == 10) recovery = recovery[..5] + "-" + recovery[5..];
        return (await users.RedeemTwoFactorRecoveryCodeAsync(user, recovery)).Succeeded;
    }
}
