using System.Security.Cryptography;
using System.Text;
using BlazorApp3.Data;
using BlazorApp3.Features.Notifications;
using BlazorApp3.Services.Email;
using Hangfire;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BlazorApp3.Features.Account;

public enum CodeIssueStatus { Sent, Cooldown, AlreadyConfirmed }
public sealed record CodeIssueResult(CodeIssueStatus Status, int CooldownSeconds);

public enum CodeVerifyStatus { Confirmed, Invalid, Expired, TooManyAttempts, AlreadyConfirmed }
public sealed record CodeVerifyResult(CodeVerifyStatus Status, int RemainingAttempts = 0);

/// <summary>
/// Emissão e validação do código de confirmação de e-mail.
/// <list type="bullet">
/// <item>Só o hash do código fica no banco; um código novo invalida os anteriores.</item>
/// <item>Validade, número de tentativas e intervalo para reenvio vêm de <see cref="AccountOptions"/>.</item>
/// <item>O envio é um job do Hangfire (com novas tentativas automáticas); o código vai cifrado (Data Protection) nos argumentos do job.</item>
/// </list>
/// </summary>
public sealed class EmailConfirmationService(
    AppDbContext db,
    UserManager<AppUser> users,
    IBackgroundJobClient jobs,
    INotifier notifier,
    IDataProtectionProvider dataProtection,
    IOptions<AccountOptions> options,
    IOptions<EmailOptions> emailOptions,
    IWebHostEnvironment env,
    ILogger<EmailConfirmationService> logger)
{
    public const string ProtectorPurpose = "EmailConfirmationCode";
    private AccountOptions O => options.Value;

    /// <summary>
    /// Confirmação sem código: SOMENTE em Development e quando nenhum provedor de e-mail real está configurado
    /// (ver <see cref="EmailOptions.IsDeliveryConfigured"/>). Em qualquer outro cenário é sempre false.
    /// </summary>
    public bool DevAutoConfirmAvailable => env.IsDevelopment() && !emailOptions.Value.IsDeliveryConfigured();

    /// <summary>Confirma o e-mail sem código (atalho de desenvolvimento). Lança se <see cref="DevAutoConfirmAvailable"/> for false.</summary>
    public async Task<CodeVerifyResult> DevAutoConfirmAsync(AppUser user, CancellationToken ct = default)
    {
        if (!DevAutoConfirmAvailable)
            throw new InvalidOperationException("Confirmação automática só é permitida em Development sem provedor de e-mail configurado.");
        if (user.EmailConfirmed) return new(CodeVerifyStatus.AlreadyConfirmed);

        var now = DateTime.UtcNow;
        await db.EmailConfirmationCodes
            .Where(c => c.UserId == user.Id && c.ConsumedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.ConsumedAt, now), ct);

        await MarkConfirmedAsync(user);
        logger.LogWarning("E-mail de {Email} confirmado automaticamente (modo desenvolvimento, sem provedor de e-mail).", user.Email);
        return new(CodeVerifyStatus.Confirmed);
    }

    public async Task<CodeIssueResult> IssueCodeAsync(AppUser user, CancellationToken ct = default)
    {
        if (user.EmailConfirmed) return new(CodeIssueStatus.AlreadyConfirmed, 0);

        var remaining = await CooldownRemainingAsync(user.Id, ct);
        if (remaining > 0) return new(CodeIssueStatus.Cooldown, remaining);

        var now = DateTime.UtcNow;
        await db.EmailConfirmationCodes
            .Where(c => c.UserId == user.Id && c.ConsumedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.ConsumedAt, now), ct);

        var max = (int)Math.Pow(10, O.CodeLength);
        var code = RandomNumberGenerator.GetInt32(0, max).ToString("D" + O.CodeLength);
        var lifetime = TimeSpan.FromMinutes(O.CodeLifetimeMinutes);
        db.EmailConfirmationCodes.Add(new EmailConfirmationCode
        {
            UserId = user.Id, CodeHash = Hash(user.Id, code), CreatedAt = now, ExpiresAt = now + lifetime
        });
        await db.SaveChangesAsync(ct);

        var protectedCode = dataProtection.CreateProtector(ProtectorPurpose).ToTimeLimitedDataProtector().Protect(code, lifetime);
        jobs.Enqueue<AccountEmailJobs>(j => j.SendConfirmationCodeAsync(user.Id, protectedCode));
        return new(CodeIssueStatus.Sent, O.ResendCooldownSeconds);
    }

    /// <summary>Segundos até poder pedir outro código (0 = liberado).</summary>
    public async Task<int> CooldownRemainingAsync(string userId, CancellationToken ct = default)
    {
        var last = await db.EmailConfirmationCodes.Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt).Select(c => (DateTime?)c.CreatedAt).FirstOrDefaultAsync(ct);
        if (last is null) return 0;
        var elapsed = DateTime.UtcNow - last.Value;
        return Math.Max(0, (int)Math.Ceiling(O.ResendCooldownSeconds - elapsed.TotalSeconds));
    }

    public async Task<CodeVerifyResult> VerifyAsync(AppUser user, string code, CancellationToken ct = default)
    {
        if (user.EmailConfirmed) return new(CodeVerifyStatus.AlreadyConfirmed);

        var current = await db.EmailConfirmationCodes.Where(c => c.UserId == user.Id && c.ConsumedAt == null)
            .OrderByDescending(c => c.CreatedAt).FirstOrDefaultAsync(ct);
        if (current is null || current.ExpiresAt <= DateTime.UtcNow) return new(CodeVerifyStatus.Expired);
        if (current.Attempts >= O.MaxCodeAttempts) return new(CodeVerifyStatus.TooManyAttempts);

        current.Attempts++;
        var normalized = new string((code ?? "").Where(char.IsDigit).ToArray());
        var ok = CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Hash(user.Id, normalized)), Encoding.ASCII.GetBytes(current.CodeHash));

        if (!ok)
        {
            await db.SaveChangesAsync(ct);
            var left = O.MaxCodeAttempts - current.Attempts;
            return left <= 0 ? new(CodeVerifyStatus.TooManyAttempts) : new(CodeVerifyStatus.Invalid, left);
        }

        current.ConsumedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await MarkConfirmedAsync(user);
        return new(CodeVerifyStatus.Confirmed);
    }

    private async Task MarkConfirmedAsync(AppUser user)
    {
        user.EmailConfirmed = true;
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        notifier.Welcome(user);
    }

    private static string Hash(string userId, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{userId}:{code}")));
}
