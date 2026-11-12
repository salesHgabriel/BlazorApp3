using BlazorApp3.Localization;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Services;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp3.Features.Users;

internal sealed class CreateUserHandler(UserManager<AppUser> users, AppDbContext db) : ICommandHandler<CreateUserCommand, string>
{
    public async Task<string> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        var m = command.Model;
        if (string.IsNullOrWhiteSpace(m.Password))
            throw new AppException(AppText.Get("Informe uma senha para o novo usuário."));

        var user = new AppUser
        {
            UserName = m.Email.Trim(), Email = m.Email.Trim(), EmailConfirmed = true,
            FullName = m.FullName.Trim(), JobTitle = m.JobTitle, Department = m.Department,
            AvatarUrl = m.AvatarUrl, IsActive = m.IsActive, CreatedAt = DateTime.UtcNow
        };
        AppException.ThrowIfFailed(await users.CreateAsync(user, m.Password));
        await UserAssignments.SyncRolesAsync(users, db, user, m.RoleIds, ct);
        await UserAssignments.SyncTeamsAsync(db, user.Id, m.TeamIds, ct);
        return user.Id;
    }
}
