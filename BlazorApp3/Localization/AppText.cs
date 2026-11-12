using System.Globalization;
using System.Resources;

namespace BlazorApp3.Localization;

/// <summary>
/// Acesso estático aos mesmos resource files do <c>IStringLocalizer&lt;SharedResource&gt;</c>.
/// Usado onde não há injeção de dependência: atributos de validação e helpers estáticos.
/// </summary>
public static class AppText
{
    private static readonly ResourceManager Resources = new("BlazorApp3.Resources.SharedResource", typeof(SharedResource).Assembly);

    /// <summary>Texto traduzido para a cultura atual; a própria chave (pt-BR) se não houver tradução.</summary>
    public static string Get(string key)
    {
        try
        {
            return Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;
        }
        catch (MissingManifestResourceException)
        {
            return key;
        }
    }

    public static string Format(string key, params object?[] args) => string.Format(CultureInfo.CurrentCulture, Get(key), args);
}
