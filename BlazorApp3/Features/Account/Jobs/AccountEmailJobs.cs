using System.Security.Cryptography;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Features.Branding;
using BlazorApp3.Localization;
using BlazorApp3.Services;
using BlazorApp3.Services.Email;
using Hangfire;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace BlazorApp3.Features.Account;

/// <summary>
/// E-mail transacional do código de confirmação (Hangfire, fila "emails"). É sempre enviado, independente
/// das configurações de notificação. Falhas do provedor lançam exceção e o Hangfire tenta de novo.
/// Os demais avisos da conta (boas-vindas, bloqueio, alerta de segurança) passam pelo INotifier.
/// </summary>
[Queue(JobQueues.Emails)]
[AutomaticRetry(Attempts = 5)]
public sealed class AccountEmailJobs(
    UserManager<AppUser> users,
    IDispatcher dispatcher,
    IMailSender mail,
    IDataProtectionProvider dataProtection,
    AppUrls urls,
    IOptions<AccountOptions> options,
    ILogger<AccountEmailJobs> logger)
{
    [JobDisplayName("E-mail: código de confirmação ({0})")]
    public async Task SendConfirmationCodeAsync(string userId, string protectedCode)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null || user.EmailConfirmed) return;

        string code;
        try
        {
            code = dataProtection.CreateProtector(EmailConfirmationService.ProtectorPurpose).ToTimeLimitedDataProtector().Unprotect(protectedCode);
        }
        catch (CryptographicException)
        {
            logger.LogWarning("Código de confirmação de {UserId} expirou antes do envio; e-mail descartado.", userId);
            return; // expirado: não adianta tentar de novo
        }

        var brand = await dispatcher.QueryAsync(new GetBrandingQuery());
        var link = urls.Absolute($"Account/ConfirmEmail?email={Uri.EscapeDataString(user.Email!)}&code={code}");
        var content = new EmailTemplate.Content(
            AppText.Get("Confirme seu e-mail"),
            [
                AppText.Format("Olá, {0}!", EmailTemplate.FirstName(user.FullName)),
                AppText.Get("Use o código abaixo para confirmar seu e-mail e ativar sua conta:")
            ])
        {
            Code = code,
            ButtonText = AppText.Get("Confirmar e-mail"),
            ButtonUrl = link,
            Footnote = AppText.Format("O código expira em {0} minutos. Se você não criou esta conta, ignore este e-mail.", options.Value.CodeLifetimeMinutes)
        };
        await mail.SendAsync(user.Email!, user.FullName, AppText.Get("Seu código de confirmação"), EmailTemplate.Render(brand, content), brand.AppName);
    }
}
