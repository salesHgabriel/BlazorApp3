using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Users;

internal sealed class GetUserForEditHandler(AppDbContext db) : IQueryHandler<GetUserForEditQuery, UserEditModel?>
{
    public async Task<UserEditModel?> HandleAsync(GetUserForEditQuery query, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.Id, ct);
        if (user is null) return null;
        return new UserEditModel
        {
            Id = user.Id, FullName = user.FullName, Email = user.Email!, JobTitle = user.JobTitle,
            Department = user.Department, AvatarUrl = user.AvatarUrl, IsActive = user.IsActive,
            RoleIds = await db.UserRoles.Where(ur => ur.UserId == user.Id).Select(ur => ur.RoleId).ToListAsync(ct),
            TeamIds = await db.TeamMembers.Where(tm => tm.UserId == user.Id).Select(tm => tm.TeamId).ToListAsync(ct)
        };
    }
}
