using System.Globalization;

namespace BlazorApp3.Localization;

/// <summary>
/// Formatação e leitura de valores monetários segundo o idioma atual.
/// O idioma define separadores e posição do símbolo; a moeda (BRL, USD, EUR) define o símbolo.
/// Não há conversão de câmbio: o mesmo valor apenas é exibido conforme as convenções de cada idioma.
/// </summary>
public static class Money
{
    public static readonly IReadOnlyList<string> Currencies = ["BRL", "USD", "EUR"];

    private static readonly Dictionary<string, (string Native, string International)> Symbols = new()
    {
        ["BRL"] = ("R$", "R$"),
        ["USD"] = ("$", "US$"),
        ["EUR"] = ("€", "€"),
    };

    /// <summary>Moeda padrão do idioma atual.</summary>
    public static string DefaultCurrency(CultureInfo? culture = null) =>
        (culture is null ? AppCultures.Current : AppCultures.All.FirstOrDefault(c => c.Name == culture.Name) ?? AppCultures.All[0]).Currency;

    /// <summary>Símbolo da moeda; "$" só é usado sem prefixo no idioma em que o dólar é a moeda padrão.</summary>
    public static string Symbol(string currency, CultureInfo? culture = null)
    {
        if (!Symbols.TryGetValue(currency, out var s)) return currency;
        return DefaultCurrency(culture) == currency ? s.Native : s.International;
    }

    /// <summary>Formato numérico do idioma com o símbolo da moeda informada.</summary>
    public static NumberFormatInfo FormatFor(string? currency = null, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        var nfi = (NumberFormatInfo)culture.NumberFormat.Clone();
        nfi.CurrencySymbol = Symbol(currency ?? DefaultCurrency(culture), culture);
        return nfi;
    }

    /// <summary>Ex.: 1234.5 → "R$ 1.234,50" (pt-BR) · "$1,234.50" (en-US) · "1.234,50 €" (es-ES).</summary>
    public static string Format(decimal value, string? currency = null, CultureInfo? culture = null) =>
        value.ToString("C2", FormatFor(currency, culture));

    /// <summary>Lê um valor digitado no formato do idioma atual (com ou sem símbolo e separador de milhar).</summary>
    public static bool TryParse(string? text, out decimal value, string? currency = null, CultureInfo? culture = null)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var nfi = FormatFor(currency, culture);
        var cleaned = text.Replace(nfi.CurrencySymbol, "").Replace(" ", " ").Trim();
        return decimal.TryParse(cleaned, NumberStyles.Currency, nfi, out value);
    }
}
