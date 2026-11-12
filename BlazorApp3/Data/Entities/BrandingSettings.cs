using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp3.Data;

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
