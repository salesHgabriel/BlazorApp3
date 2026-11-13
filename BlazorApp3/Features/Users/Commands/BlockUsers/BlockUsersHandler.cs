using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Features.Account;
using BlazorApp3.Features.Notifications;
using BlazorApp3.Services;
using BlazorApp3.Security;
using Hangfire;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp3.Features.Users;

internal sealed class BlockUsersHandler(UserManager<AppUser> users, IBackgroundJobClient jobs, INotifier notifier, UserSessions sessions) : ICommandHandler<BlockUsersCommand, Unit>
{
    public async Task<Unit> HandleAsync(BlockUsersCommand command, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        DateTimeOffset until = command.Hours is { } h ? new DateTimeOffset(now).AddHours(h) : BlockState.Permanent;
        var reason = string.IsNullOrWhiteSpace(command.Reason) ? null : command.Reason.Trim();

        foreach (var id in command.Ids)
        {
            var user = await users.FindByIdAsync(id);
            if (user is null) continue;

            user.LockoutEnabled = true;
            user.LockoutEnd = until;
            user.BlockedAt = now;
            user.BlockReason = reason;
            user.BlockedBy = command.BlockedBy;
            AppException.ThrowIfFailed(await users.UpdateAsync(user));
            await users.UpdateSecurityStampAsync(user); // derruba sessões ativas
            await sessions.RevokeAllAsync(user.Id, SessionRevokeReason.Blocked); // e os navegadores abertos, na hora

            notifier.Blocked(user);
            if (command.Hours is not null)
            {
                var ticks = now.Ticks;
                jobs.Schedule<AccountMaintenanceJobs>(j => j.ExpireBlockAsync(id, ticks), until);
            }
        }
        return Unit.Value;
    }
}
