using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Teams;

internal sealed class RemoveTeamMemberHandler(AppDbContext db) : ICommandHandler<RemoveTeamMemberCommand, Unit>
{
    public async Task<Unit> HandleAsync(RemoveTeamMemberCommand command, CancellationToken ct)
    {
        await db.TeamMembers.Where(tm => tm.TeamId == command.TeamId && tm.UserId == command.UserId).ExecuteDeleteAsync(ct);
        await db.Users.Where(u => u.Id == command.UserId && u.ActiveTeamId == command.TeamId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.ActiveTeamId, (int?)null), ct);
        return Unit.Value;
    }
}
