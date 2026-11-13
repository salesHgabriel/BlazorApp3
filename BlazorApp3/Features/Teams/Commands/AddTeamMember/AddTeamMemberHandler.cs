using BlazorApp3.Localization;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Features.Notifications;
using BlazorApp3.Services;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Teams;

internal sealed class AddTeamMemberHandler(AppDbContext db, INotifier notifier) : ICommandHandler<AddTeamMemberCommand, Unit>
{
    public async Task<Unit> HandleAsync(AddTeamMemberCommand command, CancellationToken ct)
    {
        if (await db.TeamMembers.AnyAsync(tm => tm.TeamId == command.TeamId && tm.UserId == command.UserId, ct))
            throw new AppException(AppText.Get("Este usuário já é membro do time."));
        db.TeamMembers.Add(new TeamMember { TeamId = command.TeamId, UserId = command.UserId, Role = command.Role, JoinedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);

        var team = await db.Teams.AsNoTracking().Where(t => t.Id == command.TeamId).Select(t => t.Name).FirstOrDefaultAsync(ct);
        notifier.Publish(new NotificationRequest
        {
            UserIds = [command.UserId],
            Title = AppText.Get("Você entrou em um time"),
            Message = AppText.Format("Você foi adicionado(a) ao time {0}.", team ?? "—"),
            Icon = "bi-diagram-3", Level = NotificationLevel.Info, Link = $"teams/{command.TeamId}"
        });
        return Unit.Value;
    }
}
