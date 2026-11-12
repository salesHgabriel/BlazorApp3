using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Services;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Teams;

internal sealed class TeamOptionsHandler(AppDbContext db) : IQueryHandler<TeamOptionsQuery, List<SelectOption<int>>>
{
    public Task<List<SelectOption<int>>> HandleAsync(TeamOptionsQuery query, CancellationToken ct) =>
        db.Teams.OrderBy(t => t.Name).Select(t => new SelectOption<int>(t.Id, t.Name, t.PrimaryColor)).ToListAsync(ct);
}
