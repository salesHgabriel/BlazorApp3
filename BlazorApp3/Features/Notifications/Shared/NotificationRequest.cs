namespace BlazorApp3.Features.Notifications;

/// <summary>
/// Pedido de notificação (serializado como argumento do job do Hangfire).
/// Destinatários: <see cref="UserIds"/> e/ou todos com a permissão <see cref="Permission"/>.
/// </summary>
public sealed class NotificationRequest
{
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Icon { get; set; } = "bi-bell";
    /// <summary>info | success | warning | danger</summary>
    public string Level { get; set; } = NotificationLevel.Info;
    /// <summary>Caminho relativo aberto ao clicar (ex.: "users").</summary>
    public string? Link { get; set; }

    public List<string> UserIds { get; set; } = [];
    public string? Permission { get; set; }

    /// <summary>Canal "no sistema" (sino + SignalR).</summary>
    public bool InApp { get; set; } = true;
    /// <summary>Canal e-mail.</summary>
    public bool Email { get; set; } = true;
    /// <summary>Alerta de segurança: respeita a configuração "Alertas de segurança".</summary>
    public bool Security { get; set; }
    /// <summary>Entrega mesmo para contas inativas/bloqueadas (ex.: aviso de bloqueio).</summary>
    public bool IncludeInactive { get; set; }

    /// <summary>Exibido no painel do Hangfire.</summary>
    public override string ToString() => Title;
}

public static class NotificationLevel
{
    public const string Info = "info";
    public const string Success = "success";
    public const string Warning = "warning";
    public const string Danger = "danger";
}

/// <summary>Formato enviado ao navegador pelo SignalR e exibido no sino.</summary>
public sealed record NotificationDto(int Id, string Title, string Message, string Icon, string Level, string? Link, DateTime CreatedAt, bool Read);
