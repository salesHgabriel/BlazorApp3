using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp3.Data;

public class Team
{
    public int Id { get; set; }
    [MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(100)] public string Slug { get; set; } = "";
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Branding personalizado do time (sobrepõe o whitelabel global quando habilitado)
    public bool UseCustomBranding { get; set; }
    [MaxLength(100)] public string? DisplayName { get; set; }
    [MaxLength(300)] public string? LogoUrl { get; set; }
    [MaxLength(9)] public string PrimaryColor { get; set; } = "#4f46e5";
    [MaxLength(9)] public string? AccentColor { get; set; }
    [MaxLength(9)] public string? SidebarColor { get; set; }

    public ICollection<TeamMember> Members { get; set; } = [];
}
