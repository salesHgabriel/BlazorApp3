using BlazorApp3.Application.Cqrs;
using BlazorApp3.Features.Notifications;
using Hangfire;

namespace BlazorApp3.Features.Settings;

/// <summary>Enfileira (Hangfire) um e-mail de teste para o próprio usuário, pelo provedor configurado.</summary>
public sealed record SendTestEmailCommand(string UserId) : ICommand;

internal sealed class SendTestEmailHandler(IBackgroundJobClient jobs) : ICommandHandler<SendTestEmailCommand, Unit>
{
    public Task<Unit> HandleAsync(SendTestEmailCommand command, CancellationToken ct)
    {
        jobs.Enqueue<NotificationJobs>(j => j.SendTestEmailAsync(command.UserId));
        return Task.FromResult(Unit.Value);
    }
}

/// <summary>Envia uma notificação de teste (sistema + e-mail) para o próprio usuário.</summary>
public sealed record SendTestNotificationCommand(string UserId, string Title, string Message) : ICommand;

internal sealed class SendTestNotificationHandler(INotifier notifier) : ICommandHandler<SendTestNotificationCommand, Unit>
{
    public Task<Unit> HandleAsync(SendTestNotificationCommand command, CancellationToken ct)
    {
        notifier.Publish(new NotificationRequest
        {
            UserIds = [command.UserId], Title = command.Title, Message = command.Message,
            Icon = "bi-stars", Level = NotificationLevel.Info, Link = "settings/system", Email = false
        });
        return Task.FromResult(Unit.Value);
    }
}
