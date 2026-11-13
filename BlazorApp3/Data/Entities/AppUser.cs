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

    // ---- Bloqueio administrativo (o bloqueio em si é o lockout do Identity: LockoutEnd)
    /// <summary>Quando um administrador bloqueou a conta (null = sem bloqueio administrativo).</summary>
    public DateTime? BlockedAt { get; set; }
    [MaxLength(300)] public string? BlockReason { get; set; }
    [MaxLength(150)] public string? BlockedBy { get; set; }

    /// <summary>Quando o MFA (app autenticador) foi ativado. O flag em si é o TwoFactorEnabled do Identity.</summary>
    public DateTime? MfaEnabledAt { get; set; }

    public ICollection<UserSession> Sessions { get; set; } = [];
    public ICollection<EmailConfirmationCode> ConfirmationCodes { get; set; } = [];

    /// <summary>Time selecionado pelo usuário (aplica o branding personalizado do time).</summary>
    public int? ActiveTeamId { get; set; }

    public ICollection<TeamMember> Teams { get; set; } = [];
}
