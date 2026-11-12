using BlazorApp3.Localization;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Services;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Teams;

internal sealed class DeleteTeamHandler(AppDbContext db) : ICommandHandler<DeleteTeamCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeleteTeamCommand command, CancellationToken ct)
    {
        if (await db.Teams.Where(t => t.Id == command.Id).ExecuteDeleteAsync(ct) == 0)
            throw new AppException(AppText.Get("Time não encontrado."));
        // quem estava com este time ativo volta para o branding da plataforma
        await db.Users.Where(u => u.ActiveTeamId == command.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.ActiveTeamId, (int?)null), ct);
        return Unit.Value;
    }
}
