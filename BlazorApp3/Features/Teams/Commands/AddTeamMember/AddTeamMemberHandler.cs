using BlazorApp3.Localization;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Services;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Teams;

internal sealed class AddTeamMemberHandler(AppDbContext db) : ICommandHandler<AddTeamMemberCommand, Unit>
{
    public async Task<Unit> HandleAsync(AddTeamMemberCommand command, CancellationToken ct)
    {
        if (await db.TeamMembers.AnyAsync(tm => tm.TeamId == command.TeamId && tm.UserId == command.UserId, ct))
            throw new AppException(AppText.Get("Este usuário já é membro do time."));
        db.TeamMembers.Add(new TeamMember { TeamId = command.TeamId, UserId = command.UserId, Role = command.Role, JoinedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
