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

/// <summary>Role criada dinamicamente. Agrupa permissões fixas.</summary>
public class AppRole : IdentityRole
{
    public AppRole() { }
    public AppRole(string name) : base(name) { }

    [MaxLength(300)] public string? Description { get; set; }
    [MaxLength(9)] public string Color { get; set; } = "#6366f1";

    /// <summary>Roles de sistema não podem ser excluídas nem renomeadas.</summary>
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RolePermission> Permissions { get; set; } = [];
}

/// <summary>Permissão com Id e nome fixos (definidos em <see cref="Security.Perm"/>).</summary>
public class Permission
{
    public int Id { get; set; }
    [MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(60)] public string Group { get; set; } = "";
    [MaxLength(200)] public string Description { get; set; } = "";
    public ICollection<RolePermission> Roles { get; set; } = [];
}

public class RolePermission
{
    public string RoleId { get; set; } = "";
    public AppRole Role { get; set; } = null!;
    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}

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

public enum TeamRole
{
    Member = 0,
    Admin = 1,
    Owner = 2
}

public class TeamMember
{
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
    public TeamRole Role { get; set; } = TeamRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Configuração whitelabel global (linha única, Id = 1).</summary>
public class BrandingSettings
{
    public int Id { get; set; } = 1;

    [Required(ErrorMessage = "Informe o nome da aplicação"), MaxLength(60)]
    public string AppName { get; set; } = "";
    [MaxLength(120)] public string? Tagline { get; set; }
    [MaxLength(300)] public string? LogoUrl { get; set; }
    [MaxLength(300)] public string? FaviconUrl { get; set; }

    [Required, RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Cor inválida")]
    public string PrimaryColor { get; set; } = "#4f46e5";
    [Required, RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Cor inválida")]
    public string AccentColor { get; set; } = "#06b6d4";
    [Required, RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Cor inválida")]
    public string SidebarColor { get; set; } = "#0f172a";
    [Required, RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Cor inválida")]
    public string SidebarTextColor { get; set; } = "#cbd5e1";

    [MaxLength(40)] public string FontFamily { get; set; } = "Inter";
    [Range(0, 24)] public int BorderRadius { get; set; } = 10;
    [MaxLength(20)] public string PageTransition { get; set; } = "fade-up";

    [MaxLength(80)] public string? LoginTitle { get; set; }
    [MaxLength(200)] public string? LoginMessage { get; set; }
    [MaxLength(200)] public string? FooterText { get; set; }
    [MaxLength(120), EmailAddress(ErrorMessage = "E-mail inválido")] public string? SupportEmail { get; set; }

    /// <summary>Permite que times sobrescrevam nome, logo e cores.</summary>
    public bool AllowTeamBranding { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public static BrandingSettings CreateDefault() => new()
    {
        Id = 1,
        AppName = "Nexus Admin",
        Tagline = "Plataforma de gestão white label",
        PrimaryColor = "#4f46e5",
        AccentColor = "#06b6d4",
        SidebarColor = "#0f172a",
        SidebarTextColor = "#cbd5e1",
        FontFamily = "Inter",
        BorderRadius = 10,
        PageTransition = "fade-up",
        LoginTitle = "Bem-vindo de volta",
        LoginMessage = "Acesse sua conta para gerenciar times, usuários e permissões.",
        FooterText = "© 2026 Nexus Admin · Todos os direitos reservados.",
        SupportEmail = "suporte@nexus.dev",
        AllowTeamBranding = true
    };

    public BrandingSettings Clone() => (BrandingSettings)MemberwiseClone();
}
