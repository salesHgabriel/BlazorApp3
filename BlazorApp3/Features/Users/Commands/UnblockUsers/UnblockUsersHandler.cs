using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Features.Account;
using BlazorApp3.Features.Notifications;
using BlazorApp3.Services;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp3.Features.Users;

internal sealed class UnblockUsersHandler(UserManager<AppUser> users, INotifier notifier) : ICommandHandler<UnblockUsersCommand, Unit>
{
    public async Task<Unit> HandleAsync(UnblockUsersCommand command, CancellationToken ct)
    {
        foreach (var id in command.Ids)
        {
            var user = await users.FindByIdAsync(id);
            if (user is null) continue;

            var wasAdminBlock = BlockState.IsAdminBlock(user);
            user.LockoutEnd = null;
            BlockState.Clear(user); // o job agendado de fim de bloqueio vê BlockedAt nulo e não faz nada
            AppException.ThrowIfFailed(await users.UpdateAsync(user));
            await users.ResetAccessFailedCountAsync(user);

            if (wasAdminBlock) notifier.Unblocked(id);
        }
        return Unit.Value;
    }
}
