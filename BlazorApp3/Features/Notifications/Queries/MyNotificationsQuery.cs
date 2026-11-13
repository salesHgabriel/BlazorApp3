using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Notifications;

public sealed record NotificationsSnapshot(IReadOnlyList<NotificationDto> Items, int UnreadCount);

/// <summary>Últimas notificações do usuário + total não lidas (sino do topo).</summary>
public sealed record MyNotificationsQuery(string UserId, int Take = 20) : IQuery<NotificationsSnapshot>;

internal sealed class MyNotificationsHandler(AppDbContext db) : IQueryHandler<MyNotificationsQuery, NotificationsSnapshot>
{
    public async Task<NotificationsSnapshot> HandleAsync(MyNotificationsQuery query, CancellationToken ct)
    {
        var mine = db.Notifications.AsNoTracking().Where(n => n.UserId == query.UserId);
        var items = await mine.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id).Take(query.Take)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.Icon, n.Level, n.Link, n.CreatedAt, n.ReadAt != null))
            .ToListAsync(ct);
        var unread = await mine.CountAsync(n => n.ReadAt == null, ct);
        return new(items, unread);
    }
}
