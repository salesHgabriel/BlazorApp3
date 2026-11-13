using System.Text;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Features.Branding;
using BlazorApp3.Localization;
using BlazorApp3.Security;
using BlazorApp3.Services;
using Microsoft.AspNetCore.Identity;
using QRCoder;

namespace BlazorApp3.Features.Account;

// =====================================================================
// MFA (TOTP: Google Authenticator, Microsoft Authenticator, Authy, 1Password...)
//
// Ativar/desativar NÃO troca o security stamp (o UserManager trocaria e o próprio circuito/cookie atual cairia).
// No lugar disso as OUTRAS sessões são encerradas explicitamente (UserSessions), e a atual continua.
// =====================================================================

public sealed record MfaStatus(bool Enabled, DateTime? EnabledAt, int RecoveryCodesLeft);
public sealed record MfaSetup(string SharedKey, string QrCodeDataUri);
public sealed record MfaEnableResult(IReadOnlyList<string> RecoveryCodes, int SessionsEnded);

public sealed record GetMfaStatusQuery(string UserId) : IQuery<MfaStatus>;

/// <summary>Gera uma chave nova (MFA ainda desligado) e devolve o QR code para o app autenticador.</summary>
public sealed record StartMfaSetupCommand(string UserId) : ICommand<MfaSetup>;

/// <summary>Confirma o primeiro código do app e liga o MFA. Retorna os códigos de recuperação (exibidos uma vez).</summary>
public sealed record EnableMfaCommand(string UserId, string CurrentSessionId, string Code) : ICommand<MfaEnableResult>;

/// <summary>Desliga o MFA (exige senha + código).</summary>
public sealed record DisableMfaCommand(string UserId, string CurrentSessionId, string Password, string Code) : ICommand<int>
{
    public override string ToString() => $"{nameof(DisableMfaCommand)} {{ UserId = {UserId} }}"; // sem senha em logs
}

/// <summary>Gera novos códigos de recuperação (os anteriores deixam de valer; exige senha + código).</summary>
public sealed record RegenerateRecoveryCodesCommand(string UserId, string Password, string Code) : ICommand<IReadOnlyList<string>>
{
    public override string ToString() => $"{nameof(RegenerateRecoveryCodesCommand)} {{ UserId = {UserId} }}";
}

/// <summary>Administrador remove o MFA de um usuário (ex.: perdeu o celular). Encerra as sessões dele.</summary>
public sealed record ResetUserMfaCommand(string UserId) : ICommand;

internal static class Mfa
{
    public const int RecoveryCodeCount = 10;

    public static IUserAuthenticatorKeyStore<AppUser> KeyStore(IUserStore<AppUser> store) =>
        (IUserAuthenticatorKeyStore<AppUser>)store;

    public static IUserTwoFactorStore<AppUser> TwoFactorStore(IUserStore<AppUser> store) =>
        (IUserTwoFactorStore<AppUser>)store;

    /// <summary>Liga/desliga sem trocar o security stamp (ver comentário no topo do arquivo).</summary>
    public static async Task SetEnabledAsync(UserManager<AppUser> users, IUserStore<AppUser> store, AppUser user, bool enabled, CancellationToken ct)
    {
        await TwoFactorStore(store).SetTwoFactorEnabledAsync(user, enabled, ct);
        user.MfaEnabledAt = enabled ? DateTime.UtcNow : null;
        AppException.ThrowIfFailed(await users.UpdateAsync(user));
        // Sem MFA a chave antiga não deve continuar valendo (uma nova é gerada ao configurar de novo)
        if (!enabled) await users.RemoveAuthenticationTokenAsync(user, "[AspNetUserStore]", "AuthenticatorKey");
    }

    public static string FormatKey(string key)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < key.Length; i += 4) sb.Append(key.AsSpan(i, Math.Min(4, key.Length - i))).Append(' ');
        return sb.ToString().TrimEnd().ToLowerInvariant();
    }
}

internal sealed class GetMfaStatusHandler(UserManager<AppUser> users) : IQueryHandler<GetMfaStatusQuery, MfaStatus>
{
    public async Task<MfaStatus> HandleAsync(GetMfaStatusQuery query, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(query.UserId) ?? throw new AppException(AppText.Get("Usuário não encontrado."));
        var left = user.TwoFactorEnabled ? await users.CountRecoveryCodesAsync(user) : 0;
        return new MfaStatus(user.TwoFactorEnabled, user.MfaEnabledAt, left);
    }
}

internal sealed class StartMfaSetupHandler(UserManager<AppUser> users, IUserStore<AppUser> store, IDispatcher dispatcher)
    : ICommandHandler<StartMfaSetupCommand, MfaSetup>
{
    public async Task<MfaSetup> HandleAsync(StartMfaSetupCommand command, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(command.UserId) ?? throw new AppException(AppText.Get("Usuário não encontrado."));
        if (user.TwoFactorEnabled) throw new AppException(AppText.Get("A verificação em duas etapas já está ativa."));

        // Sempre uma chave nova: uma configuração abandonada não fica valendo
        await Mfa.KeyStore(store).SetAuthenticatorKeyAsync(user, users.GenerateNewAuthenticatorKey(), ct);
        AppException.ThrowIfFailed(await users.UpdateAsync(user));
        var key = await users.GetAuthenticatorKeyAsync(user) ?? throw new AppException(AppText.Get("Não foi possível gerar a chave."));

        var issuer = (await dispatcher.QueryAsync(new GetBrandingQuery(), ct)).AppName;
        var uri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(user.Email!)}" +
                  $"?secret={key}&issuer={Uri.EscapeDataString(issuer)}&digits=6";

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(6);
        return new MfaSetup(Mfa.FormatKey(key), "data:image/png;base64," + Convert.ToBase64String(png));
    }
}

internal sealed class EnableMfaHandler(UserManager<AppUser> users, IUserStore<AppUser> store, UserSessions sessions)
    : ICommandHandler<EnableMfaCommand, MfaEnableResult>
{
    public async Task<MfaEnableResult> HandleAsync(EnableMfaCommand command, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(command.UserId) ?? throw new AppException(AppText.Get("Usuário não encontrado."));
        if (user.TwoFactorEnabled) throw new AppException(AppText.Get("A verificação em duas etapas já está ativa."));

        var code = command.Code.Replace(" ", "").Replace("-", "");
        if (!await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, code))
            throw new AppException(AppText.Get("Código de verificação inválido. Confira o horário do celular e tente de novo."));

        await Mfa.SetEnabledAsync(users, store, user, true, ct);
        var codes = (await users.GenerateNewTwoFactorRecoveryCodesAsync(user, Mfa.RecoveryCodeCount))?.ToList() ?? [];
        var ended = await sessions.RevokeAllAsync(user.Id, SessionRevokeReason.Security, exceptSessionId: command.CurrentSessionId, bySessionId: command.CurrentSessionId);
        return new MfaEnableResult(codes, ended);
    }
}

internal sealed class DisableMfaHandler(UserManager<AppUser> users, IUserStore<AppUser> store, ReauthVerifier reauth, UserSessions sessions)
    : ICommandHandler<DisableMfaCommand, int>
{
    public async Task<int> HandleAsync(DisableMfaCommand command, CancellationToken ct)
    {
        var user = await reauth.VerifyAsync(command.UserId, command.Password, command.Code);
        if (!user.TwoFactorEnabled) return 0;
        await Mfa.SetEnabledAsync(users, store, user, false, ct);
        await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 0); // apaga os códigos de recuperação
        return await sessions.RevokeAllAsync(user.Id, SessionRevokeReason.Security, exceptSessionId: command.CurrentSessionId, bySessionId: command.CurrentSessionId);
    }
}

internal sealed class RegenerateRecoveryCodesHandler(UserManager<AppUser> users, ReauthVerifier reauth)
    : ICommandHandler<RegenerateRecoveryCodesCommand, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> HandleAsync(RegenerateRecoveryCodesCommand command, CancellationToken ct)
    {
        var user = await reauth.VerifyAsync(command.UserId, command.Password, command.Code);
        if (!user.TwoFactorEnabled) throw new AppException(AppText.Get("A verificação em duas etapas não está ativa."));
        return (await users.GenerateNewTwoFactorRecoveryCodesAsync(user, Mfa.RecoveryCodeCount))?.ToList() ?? [];
    }
}

internal sealed class ResetUserMfaHandler(UserManager<AppUser> users, IUserStore<AppUser> store, UserSessions sessions)
    : ICommandHandler<ResetUserMfaCommand, Unit>
{
    public async Task<Unit> HandleAsync(ResetUserMfaCommand command, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(command.UserId) ?? throw new AppException(AppText.Get("Usuário não encontrado."));
        if (!user.TwoFactorEnabled) return Unit.Value;
        await Mfa.SetEnabledAsync(users, store, user, false, ct);
        await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 0);
        await sessions.RevokeAllAsync(user.Id, SessionRevokeReason.Security);
        return Unit.Value;
    }
}
