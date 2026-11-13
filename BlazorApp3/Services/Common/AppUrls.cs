namespace BlazorApp3.Services;

/// <summary>
/// URL pública da aplicação para links em e-mails gerados fora de uma requisição (jobs).
/// Usa <c>App:PublicUrl</c> se configurado; senão, a primeira requisição recebida.
/// </summary>
public sealed class AppUrls(IConfiguration configuration)
{
    private readonly string? _configured = configuration["App:PublicUrl"];
    private string? _captured;

    public string Base => EnsureSlash(_configured ?? _captured ?? "https://localhost/");

    public string Absolute(string? path) =>
        string.IsNullOrEmpty(path) ? Base : path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? path : Base + path.TrimStart('/');

    public void Capture(HttpRequest request) =>
        _captured ??= $"{request.Scheme}://{request.Host}{request.PathBase}/";

    private static string EnsureSlash(string url) => url.EndsWith('/') ? url : url + "/";
}
