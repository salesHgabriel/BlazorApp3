using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Features.Branding;
using BlazorApp3.Features.Settings;
using BlazorApp3.Localization;
using BlazorApp3.Services;
using BlazorApp3.Services.Email;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Notifications;

/// <summary>
/// Entrega de notificações (Hangfire). <see cref="DeliverAsync"/> resolve os destinatários, grava as notificações,
/// empurra via SignalR e enfileira um job de e-mail por destinatário (falha de um não reenvia para os outros).
/// Os canais respeitam as configurações do sistema.
/// </summary>
public sealed class NotificationJobs(
    AppDbContext db,
    SystemSettingsProvider settings,
    IHubContext<NotificationsHub> hub,
    IBackgroundJobClient jobs,
    IMailSender mail,
    IDispatcher dispatcher,
    AppUrls urls,
    ILogger<NotificationJobs> logger)
{
    [JobDisplayName("Notificação: {0}")]
    [AutomaticRetry(Attempts = 3)]
    public async Task DeliverAsync(NotificationRequest request)
    {
        var s = await settings.GetAsync();
        if (request.Security && !s.SecurityAlertsEnabled) return;

        var recipients = await ResolveRecipientsAsync(request);
        if (recipients.Count == 0) return;

        if (request.InApp && s.InAppNotificationsEnabled)
        {
            var now = DateTime.UtcNow;
            var rows = recipients.Select(r => new Notification
            {
                UserId = r.Id, Title = Cut(request.Title, 150), Message = Cut(request.Message, 500), Icon = request.Icon,
                Level = request.Level, Link = request.Link, CreatedAt = now
            }).ToList();
            db.Notifications.AddRange(rows);
            await db.SaveChangesAsync();

            foreach (var n in rows)
            {
                var dto = new NotificationDto(n.Id, n.Title, n.Message, n.Icon, n.Level, n.Link, n.CreatedAt, false);
                try { await hub.Clients.User(n.UserId).SendAsync(NotificationsHub.NotificationEvent, dto); }
                catch (Exception ex) { logger.LogWarning(ex, "Falha ao enviar notificação em tempo real para {UserId}.", n.UserId); }
            }
        }

        if (request.Email && s.EmailNotificationsEnabled)
        {
            foreach (var r in recipients)
                jobs.Enqueue<NotificationJobs>(j => j.SendEmailAsync(r.Id, request.Title, request.Message, request.Link));
        }
    }

    [JobDisplayName("E-mail de notificação: {1}")]
    [Queue(JobQueues.Emails)]
    [AutomaticRetry(Attempts = 5)]
    public async Task SendEmailAsync(string userId, string title, string message, string? link)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Email is null) return;
        var brand = await dispatcher.QueryAsync(new GetBrandingQuery());
        var content = new EmailTemplate.Content(title, [AppText.Format("Olá, {0}!", EmailTemplate.FirstName(user.FullName)), message])
        {
            ButtonText = AppText.Get("Abrir no sistema"),
            ButtonUrl = urls.Absolute(link)
        };
        await mail.SendAsync(user.Email, user.FullName, title, EmailTemplate.Render(brand, content), brand.AppName);
    }

    /// <summary>E-mail de teste disparado pela tela de configurações.</summary>
    [JobDisplayName("E-mail de teste ({0})")]
    [Queue(JobQueues.Emails)]
    [AutomaticRetry(Attempts = 0)]
    public async Task SendTestEmailAsync(string userId)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Email is null) return;
        var brand = await dispatcher.QueryAsync(new GetBrandingQuery());
        var content = new EmailTemplate.Content(AppText.Get("E-mail de teste"),
            [AppText.Format("Olá, {0}!", EmailTemplate.FirstName(user.FullName)), AppText.Format("Se você recebeu esta mensagem, o envio pelo provedor {0} está funcionando.", mail.ProviderName)]);
        await mail.SendAsync(user.Email, user.FullName, AppText.Get("E-mail de teste"), EmailTemplate.Render(brand, content), brand.AppName);
    }

    private async Task<List<(string Id, string? Email)>> ResolveRecipientsAsync(NotificationRequest request)
    {
        var ids = new HashSet<string>(request.UserIds);
        if (!string.IsNullOrEmpty(request.Permission))
        {
            var perm = request.Permission;
            var roleIds = db.RolePermissions.Where(rp => rp.Permission.Name == perm).Select(rp => rp.RoleId);
            ids.UnionWith(await db.UserRoles.Where(ur => roleIds.Contains(ur.RoleId)).Select(ur => ur.UserId).ToListAsync());
        }
        if (ids.Count == 0) return [];

        var q = db.Users.AsNoTracking().Where(u => ids.Contains(u.Id));
        if (!request.IncludeInactive) q = q.Where(u => u.IsActive);
        return (await q.Select(u => new { u.Id, u.Email }).ToListAsync()).Select(u => (u.Id, u.Email)).ToList();
    }

    private static string Cut(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
}
