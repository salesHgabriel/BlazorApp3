using Hangfire;

namespace BlazorApp3.Features.Notifications;

/// <summary>Publica notificações. A entrega (banco + SignalR + e-mail) roda em segundo plano no Hangfire.</summary>
public interface INotifier
{
    void Publish(NotificationRequest request);
}

internal sealed class Notifier(IBackgroundJobClient jobs) : INotifier
{
    public void Publish(NotificationRequest request) =>
        jobs.Enqueue<NotificationJobs>(j => j.DeliverAsync(request));
}
