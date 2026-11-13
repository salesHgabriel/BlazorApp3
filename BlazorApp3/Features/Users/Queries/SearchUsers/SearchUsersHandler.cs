using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Services;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Users;

internal sealed class SearchUsersHandler(AppDbContext db) : IQueryHandler<SearchUsersQuery, PagedResult<UserListItem>>
{
    public async Task<PagedResult<UserListItem>> HandleAsync(SearchUsersQuery query, CancellationToken ct)
    {
        var page = await Sort(Filter(db.Users.AsNoTracking(), query), query)
            .Select(ToListItem)
            .ToPagedAsync(query.Page, query.PageSize, ct);

        await LoadBadgesAsync(page.Items, ct);
        return page;
    }

    internal static readonly System.Linq.Expressions.Expression<Func<AppUser, UserListItem>> ToListItem = u => new UserListItem
    {
        Id = u.Id, FullName = u.FullName, Email = u.Email!, JobTitle = u.JobTitle, Department = u.Department,
        AvatarUrl = u.AvatarUrl, IsActive = u.IsActive, CreatedAt = u.CreatedAt, LastLoginAt = u.LastLoginAt,
        EmailConfirmed = u.EmailConfirmed, TwoFactorEnabled = u.TwoFactorEnabled, LockoutEnd = u.LockoutEnd,
        BlockedAt = u.BlockedAt, BlockReason = u.BlockReason, BlockedBy = u.BlockedBy
    };

    internal IQueryable<AppUser> Filter(IQueryable<AppUser> q, SearchUsersQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.LikePattern();
            q = q.Where(u => EF.Functions.Like(u.FullName, s, "\\") || EF.Functions.Like(u.Email!, s, "\\") || EF.Functions.Like(u.JobTitle!, s, "\\"));
        }
        if (query.RoleIds.Count > 0)
        {
            var roleIds = query.RoleIds;
            q = q.Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id && roleIds.Contains(ur.RoleId)));
        }
        if (query.TeamIds.Count > 0)
        {
            var teamIds = query.TeamIds;
            q = q.Where(u => db.TeamMembers.Any(tm => tm.UserId == u.Id && teamIds.Contains(tm.TeamId)));
        }
        if (query.Departments.Count > 0)
        {
            var deps = query.Departments;
            q = q.Where(u => u.Department != null && deps.Contains(u.Department));
        }
        var now = DateTimeOffset.UtcNow;
        q = query.Status switch
        {
            "active" => q.Where(u => u.IsActive),
            "inactive" => q.Where(u => !u.IsActive),
            "blocked" => q.Where(u => u.LockoutEnd != null && u.LockoutEnd > now),
            "pending" => q.Where(u => !u.EmailConfirmed),
            _ => q
        };
        if (query.CreatedFrom.HasValue) { var from = query.CreatedFrom.Value.Date; q = q.Where(u => u.CreatedAt >= from); }
        if (query.CreatedTo.HasValue) { var to = query.CreatedTo.Value.Date.AddDays(1); q = q.Where(u => u.CreatedAt < to); }
        return q;
    }

    internal static IQueryable<AppUser> Sort(IQueryable<AppUser> q, SearchUsersQuery query) => (query.SortBy, query.SortDesc) switch
    {
        ("email", false) => q.OrderBy(u => u.Email),
        ("email", true) => q.OrderByDescending(u => u.Email),
        ("department", false) => q.OrderBy(u => u.Department).ThenBy(u => u.FullName),
        ("department", true) => q.OrderByDescending(u => u.Department).ThenBy(u => u.FullName),
        ("created", false) => q.OrderBy(u => u.CreatedAt),
        ("created", true) => q.OrderByDescending(u => u.CreatedAt),
        ("lastLogin", false) => q.OrderBy(u => u.LastLoginAt),
        ("lastLogin", true) => q.OrderByDescending(u => u.LastLoginAt),
        (_, true) => q.OrderByDescending(u => u.FullName),
        _ => q.OrderBy(u => u.FullName)
    };

    /// <summary>Papéis e times da página atual em 2 consultas (evita N+1).</summary>
    internal async Task LoadBadgesAsync(IReadOnlyList<UserListItem> items, CancellationToken ct)
    {
        var ids = items.Select(i => i.Id).ToList();
        var roles = await (from ur in db.UserRoles
                           join r in db.Roles on ur.RoleId equals r.Id
                           where ids.Contains(ur.UserId)
                           select new { ur.UserId, r.Name, r.Color }).ToListAsync(ct);
        var teams = await (from tm in db.TeamMembers
                           join t in db.Teams on tm.TeamId equals t.Id
                           where ids.Contains(tm.UserId)
                           select new { tm.UserId, t.Name, t.PrimaryColor }).ToListAsync(ct);

        foreach (var item in items)
        {
            item.Roles = roles.Where(r => r.UserId == item.Id).Select(r => new Badge(r.Name!, r.Color)).OrderBy(b => b.Label).ToList();
            item.Teams = teams.Where(t => t.UserId == item.Id).Select(t => new Badge(t.Name, t.PrimaryColor)).OrderBy(b => b.Label).ToList();
        }
    }
}
