using System.ComponentModel.DataAnnotations;

namespace BlazorApp3.Data;

/// <summary>Notificação no sistema (sino do topo). Entregue em tempo real via SignalR.</summary>
public class Notification
{
    public int Id { get; set; }
    [MaxLength(450)] public string UserId { get; set; } = "";
    [MaxLength(150)] public string Title { get; set; } = "";
    [MaxLength(500)] public string Message { get; set; } = "";
    [MaxLength(40)] public string Icon { get; set; } = "bi-bell";
    /// <summary>info | success | warning | danger</summary>
    [MaxLength(20)] public string Level { get; set; } = "info";
    [MaxLength(300)] public string? Link { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }

    public AppUser User { get; set; } = null!;
}
