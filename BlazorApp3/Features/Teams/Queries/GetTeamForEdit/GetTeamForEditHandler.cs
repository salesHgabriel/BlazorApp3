using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Teams;

internal sealed class GetTeamForEditHandler(AppDbContext db) : IQueryHandler<GetTeamForEditQuery, TeamEditModel?>
{
    public Task<TeamEditModel?> HandleAsync(GetTeamForEditQuery query, CancellationToken ct) =>
        db.Teams.AsNoTracking().Where(t => t.Id == query.Id).Select(t => new TeamEditModel
        {
            Id = t.Id, Name = t.Name, Slug = t.Slug, Description = t.Description, IsActive = t.IsActive,
            UseCustomBranding = t.UseCustomBranding, DisplayName = t.DisplayName, LogoUrl = t.LogoUrl,
            PrimaryColor = t.PrimaryColor, AccentColor = t.AccentColor, SidebarColor = t.SidebarColor
        }).FirstOrDefaultAsync(ct);
}
