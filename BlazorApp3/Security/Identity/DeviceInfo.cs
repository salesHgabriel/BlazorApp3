namespace BlazorApp3.Security;

/// <summary>Identificação simples de navegador / sistema / tipo de dispositivo a partir do User-Agent.</summary>
public sealed record DeviceInfo(string Browser, string Os, string DeviceType)
{
    public static DeviceInfo Parse(string? ua)
    {
        if (string.IsNullOrWhiteSpace(ua)) return new("?", "?", "desktop");

        bool Has(string s) => ua.Contains(s, StringComparison.OrdinalIgnoreCase);

        var browser =
            Has("Edg/") ? "Edge" :
            Has("OPR/") || Has("Opera") ? "Opera" :
            Has("SamsungBrowser") ? "Samsung Internet" :
            Has("Firefox/") || Has("FxiOS") ? "Firefox" :
            Has("CriOS") || (Has("Chrome/") && !Has("Chromium")) ? "Chrome" :
            Has("Chromium") ? "Chromium" :
            Has("Safari/") ? "Safari" : "Navegador";

        var os =
            Has("Windows") ? "Windows" :
            Has("iPhone") || Has("iPad") || Has("iPod") ? "iOS" :
            Has("Android") ? "Android" :
            Has("Mac OS X") || Has("Macintosh") ? "macOS" :
            Has("CrOS") ? "ChromeOS" :
            Has("Linux") ? "Linux" : "?";

        var type =
            Has("iPad") || Has("Tablet") || (Has("Android") && !Has("Mobile")) ? "tablet" :
            Has("Mobi") || Has("iPhone") ? "mobile" : "desktop";

        return new(browser, os, type);
    }

    public static string Icon(string deviceType, string os) => deviceType switch
    {
        "mobile" => "bi-phone",
        "tablet" => "bi-tablet",
        _ => os == "macOS" ? "bi-laptop" : "bi-display"
    };
}
