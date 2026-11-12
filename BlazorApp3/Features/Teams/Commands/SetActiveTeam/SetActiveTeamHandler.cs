using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Teams;

internal sealed class SetActiveTeamHandler(AppDbContext db) : ICommandHandler<SetActiveTeamCommand, Unit>
{
    public async Task<Unit> HandleAsync(SetActiveTeamCommand command, CancellationToken ct)
    {
        await db.Users.Where(u => u.Id == command.UserId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.ActiveTeamId, command.TeamId), ct);
        return Unit.Value;
    }
}
