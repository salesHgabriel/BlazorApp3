using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Security;
using BlazorApp3.Services;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp3.Features.Users;

internal sealed class SetUsersActiveHandler(UserManager<AppUser> users, UserSessions sessions) : ICommandHandler<SetUsersActiveCommand, Unit>
{
    public async Task<Unit> HandleAsync(SetUsersActiveCommand command, CancellationToken ct)
    {
        foreach (var id in command.Ids)
        {
            var user = await users.FindByIdAsync(id);
            if (user is null || user.IsActive == command.Active) continue;
            user.IsActive = command.Active;
            AppException.ThrowIfFailed(await users.UpdateAsync(user));
            if (!command.Active)
            {
                await users.UpdateSecurityStampAsync(user);
                await sessions.RevokeAllAsync(user.Id, SessionRevokeReason.Blocked);
            }
        }
        return Unit.Value;
    }
}
