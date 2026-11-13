using System.Security.Claims;
using BlazorApp3.Data;
using BlazorApp3.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Notifications;

/// <summary>
/// Hub SignalR (/hubs/notifications) conectado pelo navegador. O servidor envia:
/// <list type="bullet">
/// <item><c>notification</c>: nova notificação (badge + efeito no sino);</item>
/// <item><c>sessionRevoked</c>: esta sessão foi encerrada (o navegador sai na hora);</item>
/// <item><c>newSession</c>: a conta entrou em outro dispositivo (as sessões abertas podem encerrar a nova).</item>
/// </list>
/// Clients.User usa o claim NameIdentifier do cookie; cada conexão também entra no grupo da sua sessão
/// e fica registrada em UserConnections (indicador "online agora" na tela de sessões).
/// </summary>
[Authorize]
public sealed class NotificationsHub(AppDbContext db, UserSessions sessions, ILogger<NotificationsHub> logger) : Hub
{
    public const string Path = "/hubs/notifications";
    public const string NotificationEvent = "notification";
    public const string SessionRevokedEvent = "sessionRevoked";
    public const string NewSessionEvent = "newSession";

    public override async Task OnConnectedAsync()
    {
        var user = Context.User!;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var sid = user.FindFirstValue(UserSessions.ClaimType);
        if (userId is null || sid is null || !await sessions.IsCurrentAsync(user))
        {
            await Clients.Caller.SendAsync(SessionRevokedEvent, SessionRevokeReason.Revoked);
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, UserSessions.Group(sid));
        try
        {
            db.UserConnections.Add(new UserConnection { ConnectionId = Context.ConnectionId, SessionId = sid, UserId = userId });
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) { logger.LogDebug(ex, "Conexão {ConnectionId} não registrada.", Context.ConnectionId); }
        await sessions.TouchAsync(sid);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await db.UserConnections.Where(c => c.ConnectionId == Context.ConnectionId).ExecuteDeleteAsync();
        await base.OnDisconnectedAsync(exception);
    }
}
