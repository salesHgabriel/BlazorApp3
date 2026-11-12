using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Services;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Roles;

internal sealed class PermissionCatalogHandler(AppDbContext db) : IQueryHandler<PermissionCatalogQuery, List<PermissionCatalogItem>>
{
    public async Task<List<PermissionCatalogItem>> HandleAsync(PermissionCatalogQuery query, CancellationToken ct)
    {
        var items = await db.Permissions.AsNoTracking().OrderBy(p => p.Id)
            .Select(p => new PermissionCatalogItem { Id = p.Id, Name = p.Name, Group = p.Group, Description = p.Description })
            .ToListAsync(ct);
        var links = await db.RolePermissions.Select(rp => new { rp.PermissionId, rp.Role.Name, rp.Role.Color }).ToListAsync(ct);
        foreach (var item in items)
            item.Roles = links.Where(l => l.PermissionId == item.Id).Select(l => new Badge(l.Name!, l.Color)).OrderBy(b => b.Label).ToList();
        return items;
    }
}
