using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Dashboard;

public sealed record GetDashboardQuery : IQuery<DashboardData>;

public sealed record DashboardData(
    int TotalUsers, int ActiveUsers, int NewUsers30d, int TotalTeams, int ActiveTeams, int TotalRoles,
    List<(string Label, int Count)> UsersByDepartment,
    List<(string Name, string Color, int Members)> TopTeams,
    List<UserLookup> RecentUsers);

internal sealed class DashboardHandler(AppDbContext db) : IQueryHandler<GetDashboardQuery, DashboardData>
{
    public async Task<DashboardData> HandleAsync(GetDashboardQuery query, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-30);

        var byDept = await db.Users.Where(u => u.Department != null).GroupBy(u => u.Department!)
            .Select(g => new { g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).ToListAsync(ct);
        var topTeams = await db.Teams.Select(t => new { t.Name, t.PrimaryColor, Members = t.Members.Count })
            .OrderByDescending(t => t.Members).Take(6).ToListAsync(ct);
        var recent = await db.Users.OrderByDescending(u => u.CreatedAt).Take(6)
            .Select(u => new UserLookup(u.Id, u.FullName, u.Email!, u.AvatarUrl, u.Department)).ToListAsync(ct);

        return new DashboardData(
            await db.Users.CountAsync(ct),
            await db.Users.CountAsync(u => u.IsActive, ct),
            await db.Users.CountAsync(u => u.CreatedAt >= since, ct),
            await db.Teams.CountAsync(ct),
            await db.Teams.CountAsync(t => t.IsActive, ct),
            await db.Roles.CountAsync(ct),
            byDept.Select(x => (x.Key, x.Count)).ToList(),
            topTeams.Select(x => (x.Name, x.PrimaryColor, x.Members)).ToList(),
            recent);
    }
}
