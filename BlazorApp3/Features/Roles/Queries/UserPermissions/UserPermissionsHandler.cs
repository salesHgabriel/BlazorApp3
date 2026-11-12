using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Roles;

internal sealed class UserPermissionsHandler(AppDbContext db) : IQueryHandler<UserPermissionsQuery, HashSet<string>>
{
    public async Task<HashSet<string>> HandleAsync(UserPermissionsQuery query, CancellationToken ct)
    {
        var active = await db.Users.Where(u => u.Id == query.UserId).Select(u => u.IsActive).FirstOrDefaultAsync(ct);
        if (!active) return [];

        var names = await (
            from ur in db.UserRoles
            join rp in db.RolePermissions on ur.RoleId equals rp.RoleId
            join p in db.Permissions on rp.PermissionId equals p.Id
            where ur.UserId == query.UserId
            select p.Name).Distinct().ToListAsync(ct);
        return names.ToHashSet(StringComparer.Ordinal);
    }
}
