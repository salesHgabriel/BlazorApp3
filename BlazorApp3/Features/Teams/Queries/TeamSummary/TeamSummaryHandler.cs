using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Teams;

internal sealed class TeamSummaryHandler(AppDbContext db) : IQueryHandler<TeamSummaryQuery, TeamListItem?>
{
    public Task<TeamListItem?> HandleAsync(TeamSummaryQuery query, CancellationToken ct) =>
        db.Teams.AsNoTracking().Where(t => t.Id == query.Id).ToListItems().FirstOrDefaultAsync(ct);
}
