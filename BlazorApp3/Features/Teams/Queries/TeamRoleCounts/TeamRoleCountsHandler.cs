using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Teams;

internal sealed class TeamRoleCountsHandler(AppDbContext db) : IQueryHandler<TeamRoleCountsQuery, Dictionary<TeamRole, int>>
{
    public Task<Dictionary<TeamRole, int>> HandleAsync(TeamRoleCountsQuery query, CancellationToken ct) =>
        db.TeamMembers.Where(tm => tm.TeamId == query.TeamId).GroupBy(tm => tm.Role)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), ct);
}
