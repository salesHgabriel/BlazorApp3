using BlazorApp3.Data;

namespace BlazorApp3.Features.Teams;

/// <summary>Time disponível para o usuário (seletor de time + branding).</summary>
public sealed record TeamOption(int Id, string Name, string? DisplayName, string? LogoUrl, string PrimaryColor,
    string? AccentColor, string? SidebarColor, bool UseCustomBranding, TeamRole Role);
