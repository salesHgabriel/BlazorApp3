using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace BlazorApp3.Services.Files.Storage;

/// <summary>
/// Disco local (padrão): a chave vira caminho dentro de <c>Storage:Local:RootPath</c> (App_Data), ex.:
/// <c>uploads/avatars/x.png</c> → App_Data/uploads/avatars/x.png. Não gera URL direta: os downloads passam pelo app.
/// Em vários servidores atrás de um balanceador, use um provedor de nuvem (ou um disco compartilhado).
/// </summary>
public sealed class LocalFileStorageProvider : IFileStorageProvider
{
    public const string ProviderName = "Local";
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    public LocalFileStorageProvider(IWebHostEnvironment env, IOptions<FileStorageOptions> options)
    {
        var root = options.Value.Local.RootPath;
        RootPath = Path.GetFullPath(Path.IsPathRooted(root) ? root : Path.Combine(env.ContentRootPath, root));
        Directory.CreateDirectory(RootPath);
    }

    public string Name => ProviderName;

    /// <summary>Pasta raiz (App_Data por padrão).</summary>
    public string RootPath { get; }

    /// <summary>Caminho físico de uma chave (garante que fica dentro da raiz).</summary>
    public string PathOf(string key)
    {
        var path = Path.GetFullPath(Path.Combine(RootPath, StorageKey.Normalize(key).Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(RootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Chave de armazenamento inválida.", nameof(key));
        return path;
    }

    public async Task<long> SaveAsync(string key, Stream content, string contentType, StorageVisibility visibility, CancellationToken ct = default)
    {
        var path = PathOf(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            await using var output = File.Create(path);
            await content.CopyToAsync(output, 81920, ct);
            return output.Length;
        }
        catch
        {
            File.Delete(path);
            throw;
        }
    }

    public Task<StoredFileStream?> OpenReadAsync(string key, CancellationToken ct = default)
    {
        var path = PathOf(key);
        if (!File.Exists(path)) return Task.FromResult<StoredFileStream?>(null);
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        var type = ContentTypes.TryGetContentType(path, out var t) ? t : "application/octet-stream";
        return Task.FromResult<StoredFileStream?>(new StoredFileStream(stream, type, stream.Length));
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default) => Task.FromResult(File.Exists(PathOf(key)));

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var path = PathOf(key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<string?> GetDownloadUrlAsync(string key, TimeSpan expiresIn, string? downloadFileName = null, CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
}
