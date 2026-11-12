using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp3.Data;

public class AppUser : IdentityUser
{
    [MaxLength(150)] public string FullName { get; set; } = "";
    [MaxLength(100)] public string? JobTitle { get; set; }
    [MaxLength(100)] public string? Department { get; set; }
    [MaxLength(300)] public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Time selecionado pelo usuário (aplica o branding personalizado do time).</summary>
    public int? ActiveTeamId { get; set; }

    public ICollection<TeamMember> Teams { get; set; } = [];
}
