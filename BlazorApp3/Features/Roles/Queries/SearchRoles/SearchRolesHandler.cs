using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Services;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Roles;

internal sealed class SearchRolesHandler(AppDbContext db) : IQueryHandler<SearchRolesQuery, PagedResult<RoleListItem>>
{
    public Task<PagedResult<RoleListItem>> HandleAsync(SearchRolesQuery query, CancellationToken ct)
    {
        var q = db.Roles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.LikePattern();
            q = q.Where(r => EF.Functions.Like(r.Name!, s, "\\") || EF.Functions.Like(r.Description!, s, "\\"));
        }

        var projected = q.Select(r => new RoleListItem
        {
            Id = r.Id, Name = r.Name!, Description = r.Description, Color = r.Color, IsSystem = r.IsSystem, CreatedAt = r.CreatedAt,
            UserCount = db.UserRoles.Count(ur => ur.RoleId == r.Id),
            PermissionCount = r.Permissions.Count
        });

        projected = (query.SortBy, query.SortDesc) switch
        {
            ("users", false) => projected.OrderBy(r => r.UserCount),
            ("users", true) => projected.OrderByDescending(r => r.UserCount),
            ("permissions", false) => projected.OrderBy(r => r.PermissionCount),
            ("permissions", true) => projected.OrderByDescending(r => r.PermissionCount),
            ("created", false) => projected.OrderBy(r => r.CreatedAt),
            ("created", true) => projected.OrderByDescending(r => r.CreatedAt),
            (_, true) => projected.OrderByDescending(r => r.Name),
            _ => projected.OrderByDescending(r => r.IsSystem).ThenBy(r => r.Name)
        };
        return projected.ToPagedAsync(query.Page, query.PageSize, ct);
    }
}
