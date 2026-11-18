using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace BlazorApp3.Services.Files.Storage;

/// <summary>Seção "Storage" do appsettings.</summary>
public sealed class FileStorageOptions
{
    public const string Section = "Storage";

    /// <summary>
    /// Nome do provedor (<see cref="IFileStorageProvider.Name"/>). Vazio: usa o provedor de nuvem registrado, se houver
    /// exatamente um; senão, o disco local.
    /// </summary>
    public string? Provider { get; set; }

    public LocalOptions Local { get; set; } = new();

    public sealed class LocalOptions
    {
        /// <summary>Raiz dos arquivos no disco (relativa ao ContentRoot). As chaves "uploads/..." e "reports/..." ficam dentro dela.</summary>
        public string RootPath { get; set; } = "App_Data";
    }
}

/// <summary>Escolhe o provedor uma vez (na criação) e delega a ele.</summary>
public sealed class FileStorage : IFileStorage
{
    private readonly IFileStorageProvider _provider;

    public FileStorage(IEnumerable<IFileStorageProvider> providers, IOptions<FileStorageOptions> options, ILogger<FileStorage> logger)
    {
        _provider = Select(providers.ToList(), options.Value.Provider);
        logger.LogInformation("Armazenamento de arquivos: {Provider}.", _provider.Name);
    }

    public string Name => _provider.Name;
    public bool IsLocal => _provider is LocalFileStorageProvider;
    /// <summary>Provedor local, quando ativo (usado para servir /uploads como estáticos).</summary>
    public LocalFileStorageProvider? Local => _provider as LocalFileStorageProvider;

    public Task<long> SaveAsync(string key, Stream content, string contentType, StorageVisibility visibility, CancellationToken ct = default) =>
        _provider.SaveAsync(StorageKey.Normalize(key), content, contentType, visibility, ct);

    public Task<StoredFileStream?> OpenReadAsync(string key, CancellationToken ct = default) =>
        _provider.OpenReadAsync(StorageKey.Normalize(key), ct);

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default) =>
        _provider.ExistsAsync(StorageKey.Normalize(key), ct);

    public Task DeleteAsync(string key, CancellationToken ct = default) =>
        _provider.DeleteAsync(StorageKey.Normalize(key), ct);

    public Task<string?> GetDownloadUrlAsync(string key, TimeSpan expiresIn, string? downloadFileName = null, CancellationToken ct = default) =>
        _provider.GetDownloadUrlAsync(StorageKey.Normalize(key), expiresIn, downloadFileName, ct);

    private static IFileStorageProvider Select(List<IFileStorageProvider> providers, string? name)
    {
        if (!string.IsNullOrWhiteSpace(name))
            return providers.FirstOrDefault(p => string.Equals(p.Name, name.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException(
                    $"Provedor de armazenamento '{name}' não registrado. Disponíveis: {string.Join(", ", providers.Select(p => p.Name))}.");

        // Sem nome: um provedor de nuvem implementado e registrado passa a ser usado automaticamente
        var cloud = providers.Where(p => p is not LocalFileStorageProvider).ToList();
        return cloud.Count switch
        {
            0 => providers.OfType<LocalFileStorageProvider>().First(),
            1 => cloud[0],
            _ => throw new InvalidOperationException(
                $"Há vários provedores de armazenamento registrados ({string.Join(", ", cloud.Select(p => p.Name))}). Escolha um em Storage:Provider.")
        };
    }
}

public static class FileStorageServiceCollectionExtensions
{
    /// <summary>Registra o <see cref="IFileStorage"/>, o provedor local e o serviço de uploads.</summary>
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.Section));
        services.AddFileStorageProvider<LocalFileStorageProvider>();
        services.AddSingleton<FileStorage>();
        services.AddSingleton<IFileStorage>(sp => sp.GetRequiredService<FileStorage>());
        services.AddSingleton<FileStorageService>();
        return services;
    }

    /// <summary>Adiciona um provedor (Azure Blob, S3...). Registrado como singleton: pode guardar o client do SDK.</summary>
    public static IServiceCollection AddFileStorageProvider<TProvider>(this IServiceCollection services) where TProvider : class, IFileStorageProvider
    {
        services.AddSingleton<TProvider>();
        services.AddSingleton<IFileStorageProvider>(sp => sp.GetRequiredService<TProvider>());
        return services;
    }

    /// <summary>
    /// Serve os uploads públicos em /uploads. Disco local: arquivos estáticos. Nuvem: redireciona para a URL do provedor
    /// (se ele gerar uma) ou repassa o conteúdo. A URL gravada no banco (/uploads/...) não muda ao trocar de provedor.
    /// </summary>
    public static WebApplication UseUploads(this WebApplication app)
    {
        var storage = app.Services.GetRequiredService<FileStorage>();
        if (storage.Local is { } local)
        {
            var dir = Path.Combine(local.RootPath, FileStorageService.Folder);
            Directory.CreateDirectory(dir);
            app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(dir), RequestPath = FileStorageService.RequestPath });
            return app;
        }

        app.MapGet(FileStorageService.RequestPath + "/{**path}", async (string path, IFileStorage files, HttpContext http, CancellationToken ct) =>
        {
            string key;
            try { key = StorageKey.Normalize(FileStorageService.Folder, path); }
            catch (ArgumentException) { return Results.NotFound(); }

            if (await files.GetDownloadUrlAsync(key, TimeSpan.FromHours(1), null, ct) is { } url)
            {
                // O redirecionamento fica em cache menos tempo que a validade da URL assinada
                http.Response.Headers.CacheControl = "public, max-age=1800";
                return Results.Redirect(url);
            }
            var file = await files.OpenReadAsync(key, ct);
            if (file is null) return Results.NotFound();
            // Nomes são GUIDs (o conteúdo de uma URL nunca muda): cache longo no navegador
            http.Response.Headers.CacheControl = "public, max-age=86400";
            return Results.Stream(file.Content, file.ContentType);
        }).AllowAnonymous().ExcludeFromDescription();
        return app;
    }
}
