using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BlazorApp3.Data;
using BlazorApp3.Features.Teams;

namespace BlazorApp3.Services;

/// <summary>Branding efetivo (global + sobrescrita do time ativo).</summary>
public sealed partial record EffectiveBranding(
    string AppName, string? Tagline, string? LogoUrl, string? FaviconUrl,
    string PrimaryColor, string AccentColor, string SidebarColor, string SidebarTextColor,
    string FontFamily, int BorderRadius, string PageTransition, string? FooterText, string? SupportEmail,
    string? TeamName, string? LoginTitle, string? LoginMessage)
{
    public static readonly IReadOnlyDictionary<string, string?> Fonts = new Dictionary<string, string?>
    {
        ["Inter"] = "Inter:wght@400;500;600;700",
        ["Poppins"] = "Poppins:wght@400;500;600;700",
        ["Roboto"] = "Roboto:wght@400;500;700",
        ["Nunito"] = "Nunito:wght@400;600;700",
        ["Montserrat"] = "Montserrat:wght@400;500;600;700",
        ["Plus Jakarta Sans"] = "Plus+Jakarta+Sans:wght@400;500;600;700",
        ["Open Sans"] = "Open+Sans:wght@400;600;700",
        ["Sistema"] = null
    };

    public static readonly IReadOnlyDictionary<string, string> Transitions = new Dictionary<string, string>
    {
        ["fade-up"] = "Fade + subir",
        ["fade"] = "Fade",
        ["slide"] = "Deslizar lateral",
        ["zoom"] = "Zoom",
        ["blur"] = "Desfoque",
        ["none"] = "Nenhuma"
    };

    public static EffectiveBranding From(BrandingSettings g, TeamOption? team)
    {
        var useTeam = team is not null && g.AllowTeamBranding && team.UseCustomBranding;
        return new EffectiveBranding(
            AppName: useTeam && !string.IsNullOrWhiteSpace(team!.DisplayName) ? team.DisplayName! : g.AppName,
            Tagline: g.Tagline,
            LogoUrl: useTeam && team!.LogoUrl is not null ? team.LogoUrl : g.LogoUrl,
            FaviconUrl: g.FaviconUrl,
            PrimaryColor: Color(useTeam ? team!.PrimaryColor : g.PrimaryColor, "#4f46e5"),
            AccentColor: Color(useTeam ? team!.AccentColor ?? g.AccentColor : g.AccentColor, "#06b6d4"),
            SidebarColor: Color(useTeam ? team!.SidebarColor ?? g.SidebarColor : g.SidebarColor, "#0f172a"),
            SidebarTextColor: Color(g.SidebarTextColor, "#cbd5e1"),
            FontFamily: Fonts.ContainsKey(g.FontFamily) ? g.FontFamily : "Inter",
            BorderRadius: Math.Clamp(g.BorderRadius, 0, 24),
            PageTransition: Transitions.ContainsKey(g.PageTransition) ? g.PageTransition : "fade-up",
            FooterText: g.FooterText,
            SupportEmail: g.SupportEmail,
            TeamName: team?.Name,
            LoginTitle: g.LoginTitle,
            LoginMessage: g.LoginMessage);
    }

    public string? FontHref => Fonts.GetValueOrDefault(FontFamily) is { } f
        ? $"https://fonts.googleapis.com/css2?family={f}&display=swap"
        : null;

    public string Initials => string.Concat(AppName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => char.ToUpperInvariant(w[0])));

    /// <summary>Gera as variáveis CSS do tema. Todos os valores são validados (sem injeção).</summary>
    public string ToCss()
    {
        var font = FontFamily == "Sistema" ? "system-ui" : $"'{FontFamily}'";
        var sb = new StringBuilder(":root{");
        Var(sb, "--brand-primary", PrimaryColor);
        Var(sb, "--brand-primary-rgb", Rgb(PrimaryColor));
        Var(sb, "--brand-primary-contrast", Contrast(PrimaryColor));
        Var(sb, "--brand-accent", AccentColor);
        Var(sb, "--brand-accent-rgb", Rgb(AccentColor));
        Var(sb, "--sidebar-bg", SidebarColor);
        Var(sb, "--sidebar-text", SidebarTextColor);
        Var(sb, "--radius", $"{BorderRadius}px");
        Var(sb, "--font-family", $"{font}, system-ui, -apple-system, 'Segoe UI', sans-serif");
        Var(sb, "--bs-primary", "var(--brand-primary)");
        Var(sb, "--bs-primary-rgb", Rgb(PrimaryColor));
        Var(sb, "--bs-link-color", "var(--brand-primary)");
        Var(sb, "--bs-link-color-rgb", Rgb(PrimaryColor));
        Var(sb, "--bs-link-hover-color", "var(--brand-primary)");
        Var(sb, "--bs-border-radius", "var(--radius)");
        Var(sb, "--bs-body-font-family", "var(--font-family)");
        sb.Append('}');
        return sb.ToString();
    }

    private static void Var(StringBuilder sb, string name, string value) => sb.Append(name).Append(':').Append(value).Append(';');

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")] private static partial Regex HexColor();

    public static string Color(string? value, string fallback) => value is not null && HexColor().IsMatch(value) ? value : fallback;

    public static string Rgb(string hex)
    {
        var r = int.Parse(hex.AsSpan(1, 2), NumberStyles.HexNumber);
        var g = int.Parse(hex.AsSpan(3, 2), NumberStyles.HexNumber);
        var b = int.Parse(hex.AsSpan(5, 2), NumberStyles.HexNumber);
        return $"{r},{g},{b}";
    }

    public static string Contrast(string hex)
    {
        var parts = Rgb(hex).Split(',').Select(int.Parse).ToArray();
        var luminance = (0.299 * parts[0] + 0.587 * parts[1] + 0.114 * parts[2]) / 255;
        return luminance > 0.62 ? "#111827" : "#ffffff";
    }
}
