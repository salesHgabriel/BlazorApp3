using BlazorApp3.Data;
using BlazorApp3.Features.Notifications;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BlazorApp3.Features.Account;

/// <summary>Rotinas recorrentes/agendadas de contas (Hangfire, fila "default").</summary>
[AutomaticRetry(Attempts = 3)]
public sealed class AccountMaintenanceJobs(
    AppDbContext db,
    UserManager<AppUser> users,
    IBackgroundJobClient jobs,
    INotifier notifier,
    IOptions<AccountOptions> options,
    ILogger<AccountMaintenanceJobs> logger)
{
    public const string PurgeUnconfirmedId = "contas-remover-nao-confirmadas";
    public const string PurgeCodesId = "contas-limpar-codigos-expirados";

    /// <summary>Diário: remove cadastros que nunca confirmaram o e-mail dentro do prazo.</summary>
    [JobDisplayName("Remover cadastros não confirmados")]
    [DisableConcurrentExecution(60)]
    public async Task<int> PurgeUnconfirmedAccountsAsync()
    {
        var limit = DateTime.UtcNow.AddDays(-options.Value.UnconfirmedAccountRetentionDays);
        var stale = await db.Users.Where(u => !u.EmailConfirmed && u.CreatedAt < limit && u.LastLoginAt == null).ToListAsync();
        foreach (var user in stale)
        {
            var result = await users.DeleteAsync(user);
            if (!result.Succeeded) logger.LogWarning("Falha ao remover {Email}: {Errors}", user.Email, string.Join("; ", result.Errors.Select(e => e.Description)));
        }
        if (stale.Count > 0) logger.LogInformation("{Count} cadastro(s) não confirmado(s) removido(s).", stale.Count);
        return stale.Count;
    }

    /// <summary>De hora em hora: apaga códigos de confirmação vencidos há mais de um dia.</summary>
    [JobDisplayName("Limpar códigos de confirmação expirados")]
    public Task<int> PurgeExpiredCodesAsync()
    {
        var limit = DateTime.UtcNow.AddDays(-1);
        return db.EmailConfirmationCodes.Where(c => c.ExpiresAt < limit).ExecuteDeleteAsync();
    }

    /// <summary>
    /// Agendado para o fim de um bloqueio temporário: limpa os dados do bloqueio e avisa o usuário.
    /// <paramref name="blockedAtTicks"/> identifica o bloqueio; se ele foi alterado/removido depois, o job não faz nada.
    /// </summary>
    [JobDisplayName("Encerrar bloqueio temporário ({0})")]
    public async Task ExpireBlockAsync(string userId, long blockedAtTicks)
    {
        var user = await users.FindByIdAsync(userId);
        if (user?.BlockedAt is null || user.BlockedAt.Value.Ticks != blockedAtTicks) return;
        if (user.LockoutEnd is { } end && end > DateTimeOffset.UtcNow)
        {
            // Ainda não venceu (fila atrasada adiantou ou bloqueio estendido): reagenda
            jobs.Schedule<AccountMaintenanceJobs>(j => j.ExpireBlockAsync(userId, blockedAtTicks), end);
            return;
        }

        BlockState.Clear(user);
        await users.UpdateAsync(user);
        await users.ResetAccessFailedCountAsync(user);
        notifier.Unblocked(userId);
    }
}
