using BlazorApp3.Localization;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Services;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Teams;

internal sealed class UpdateTeamMemberRoleHandler(AppDbContext db) : ICommandHandler<UpdateTeamMemberRoleCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdateTeamMemberRoleCommand command, CancellationToken ct)
    {
        var member = await db.TeamMembers.FindAsync([command.TeamId, command.UserId], ct) ?? throw new AppException(AppText.Get("Membro não encontrado."));
        if (member.Role == TeamRole.Owner && command.Role != TeamRole.Owner &&
            await db.TeamMembers.CountAsync(tm => tm.TeamId == command.TeamId && tm.Role == TeamRole.Owner, ct) == 1)
            throw new AppException(AppText.Get("O time precisa ter pelo menos um proprietário."));
        member.Role = command.Role;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
