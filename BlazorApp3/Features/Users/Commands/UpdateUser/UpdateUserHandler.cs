using BlazorApp3.Localization;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Services;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp3.Features.Users;

internal sealed class UpdateUserHandler(UserManager<AppUser> users, AppDbContext db) : ICommandHandler<UpdateUserCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdateUserCommand command, CancellationToken ct)
    {
        var m = command.Model;
        var user = await users.FindByIdAsync(m.Id!) ?? throw new AppException(AppText.Get("Usuário não encontrado."));
        var deactivated = user.IsActive && !m.IsActive;

        user.FullName = m.FullName.Trim();
        user.JobTitle = m.JobTitle;
        user.Department = m.Department;
        user.AvatarUrl = m.AvatarUrl;
        user.IsActive = m.IsActive;

        var email = m.Email.Trim();
        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            AppException.ThrowIfFailed(await users.SetEmailAsync(user, email));
            AppException.ThrowIfFailed(await users.SetUserNameAsync(user, email));
        }
        AppException.ThrowIfFailed(await users.UpdateAsync(user));

        if (!string.IsNullOrWhiteSpace(m.Password))
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            AppException.ThrowIfFailed(await users.ResetPasswordAsync(user, token, m.Password));
        }
        if (deactivated) await users.UpdateSecurityStampAsync(user); // derruba sessões ativas

        await UserAssignments.SyncRolesAsync(users, db, user, m.RoleIds, ct);
        await UserAssignments.SyncTeamsAsync(db, user.Id, m.TeamIds, ct);
        return Unit.Value;
    }
}
