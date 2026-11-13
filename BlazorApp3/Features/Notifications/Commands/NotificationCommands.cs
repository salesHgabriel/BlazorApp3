using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Notifications;

/// <summary>Marca uma notificação (Id) ou todas (Id nulo) do usuário como lidas.</summary>
public sealed record MarkNotificationsReadCommand(string UserId, int? Id = null) : ICommand;

/// <summary>Apaga as notificações já lidas do usuário.</summary>
public sealed record ClearReadNotificationsCommand(string UserId) : ICommand;

internal sealed class MarkNotificationsReadHandler(AppDbContext db) : ICommandHandler<MarkNotificationsReadCommand, Unit>
{
    public async Task<Unit> HandleAsync(MarkNotificationsReadCommand command, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var q = db.Notifications.Where(n => n.UserId == command.UserId && n.ReadAt == null);
        if (command.Id is { } id) q = q.Where(n => n.Id == id);
        await q.ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, now), ct);
        return Unit.Value;
    }
}

internal sealed class ClearReadNotificationsHandler(AppDbContext db) : ICommandHandler<ClearReadNotificationsCommand, Unit>
{
    public async Task<Unit> HandleAsync(ClearReadNotificationsCommand command, CancellationToken ct)
    {
        await db.Notifications.Where(n => n.UserId == command.UserId && n.ReadAt != null).ExecuteDeleteAsync(ct);
        return Unit.Value;
    }
}
