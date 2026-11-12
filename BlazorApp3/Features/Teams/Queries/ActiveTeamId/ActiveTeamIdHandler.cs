using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Teams;

internal sealed class ActiveTeamIdHandler(AppDbContext db) : IQueryHandler<ActiveTeamIdQuery, int?>
{
    public Task<int?> HandleAsync(ActiveTeamIdQuery query, CancellationToken ct) =>
        db.Users.Where(u => u.Id == query.UserId).Select(u => u.ActiveTeamId).FirstOrDefaultAsync(ct);
}
