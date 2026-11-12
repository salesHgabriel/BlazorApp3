using System.Globalization;

namespace BlazorApp3.Localization;

/// <summary>Idiomas suportados. O primeiro é o padrão.</summary>
public static class AppCultures
{
    public const string Default = "pt-BR";

    public static readonly IReadOnlyList<AppCulture> All =
    [
        new("pt-BR", "Português", "PT", "BRL"),
        new("en-US", "English", "EN", "USD"),
        new("es-ES", "Español", "ES", "EUR")
    ];

    public static string[] Names => All.Select(c => c.Name).ToArray();

    public static bool IsSupported(string? name) => All.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    public static AppCulture Current =>
        All.FirstOrDefault(c => c.Name == CultureInfo.CurrentUICulture.Name) ?? All[0];
}
