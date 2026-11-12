using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Roles;

internal sealed class GetRoleForEditHandler(AppDbContext db) : IQueryHandler<GetRoleForEditQuery, RoleEditModel?>
{
    public async Task<RoleEditModel?> HandleAsync(GetRoleForEditQuery query, CancellationToken ct)
    {
        var role = await db.Roles.AsNoTracking().Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == query.Id, ct);
        return role is null ? null : new RoleEditModel
        {
            Id = role.Id, Name = role.Name!, Description = role.Description, Color = role.Color, IsSystem = role.IsSystem,
            PermissionIds = role.Permissions.Select(p => p.PermissionId).ToHashSet()
        };
    }
}
