using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Services;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp3.Features.Users;

internal sealed class DeleteUsersHandler(UserManager<AppUser> users) : ICommandHandler<DeleteUsersCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeleteUsersCommand command, CancellationToken ct)
    {
        foreach (var id in command.Ids)
        {
            var user = await users.FindByIdAsync(id);
            if (user is not null) AppException.ThrowIfFailed(await users.DeleteAsync(user));
        }
        return Unit.Value;
    }
}
