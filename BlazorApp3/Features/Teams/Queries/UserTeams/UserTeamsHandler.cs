using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Teams;

internal sealed class UserTeamsHandler(AppDbContext db) : IQueryHandler<UserTeamsQuery, List<TeamOption>>
{
    public Task<List<TeamOption>> HandleAsync(UserTeamsQuery query, CancellationToken ct) =>
        db.TeamMembers.AsNoTracking()
            .Where(tm => tm.UserId == query.UserId && tm.Team.IsActive)
            .OrderBy(tm => tm.Team.Name)
            .Select(tm => new TeamOption(tm.TeamId, tm.Team.Name, tm.Team.DisplayName, tm.Team.LogoUrl, tm.Team.PrimaryColor,
                tm.Team.AccentColor, tm.Team.SidebarColor, tm.Team.UseCustomBranding, tm.Role))
            .ToListAsync(ct);
}
