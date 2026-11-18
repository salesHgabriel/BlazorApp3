using BlazorApp3.Localization;
using BlazorApp3.Services.Files.Storage;

namespace BlazorApp3.Services;

/// <summary>
/// Uploads públicos (logos, avatares, anexos), servidos em /uploads. Grava no <see cref="IFileStorage"/> configurado
/// (disco local por padrão, ou nuvem) com a chave <c>uploads/{pasta}/{guid}.{ext}</c>; a URL devolvida é sempre
/// <c>/uploads/{pasta}/{arquivo}</c>, independente do provedor.
/// </summary>
public sealed class FileStorageService(IFileStorage storage)
{
    public const string RequestPath = "/uploads";
    /// <summary>Prefixo das chaves de upload no armazenamento.</summary>
    public const string Folder = "uploads";

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".ico", ".pdf", ".txt", ".csv", ".xlsx", ".docx", ".zip"
    };

    public async Task<UploadedFile> SaveAsync(Stream input, string originalName, string contentType, string folder, long length,
        Action<double>? progress = null, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            throw new AppException(AppText.Format("Tipo de arquivo não permitido: {0}", ext));

        var safeFolder = string.Concat(folder.Where(c => char.IsLetterOrDigit(c) || c == '-')).ToLowerInvariant();
        if (safeFolder.Length == 0) safeFolder = "misc";
        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";

        await using var tracked = new ProgressReadStream(input, length, progress);
        var written = await storage.SaveAsync(StorageKey.Normalize(Folder, safeFolder, fileName), tracked, contentType, StorageVisibility.Public, ct);
        return new UploadedFile(originalName, $"{RequestPath}/{safeFolder}/{fileName}", written, contentType);
    }

    /// <summary>Chave no armazenamento de uma URL /uploads/... (null se não for um upload).</summary>
    public static string? KeyFromUrl(string? url)
    {
        if (string.IsNullOrEmpty(url) || !url.StartsWith(RequestPath + "/", StringComparison.OrdinalIgnoreCase)) return null;
        try { return StorageKey.Normalize(Folder, url[(RequestPath.Length + 1)..].Split('?')[0]); }
        catch (ArgumentException) { return null; }
    }

    /// <summary>Repassa a leitura e informa o progresso (o provedor lê o stream no seu ritmo, inclusive SDKs de nuvem).</summary>
    private sealed class ProgressReadStream(Stream inner, long length, Action<double>? progress) : Stream
    {
        private long _read;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => length > 0 ? length : throw new NotSupportedException();
        public override long Position { get => _read; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count) => Track(inner.Read(buffer, offset, count));

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) =>
            Track(await inner.ReadAsync(buffer, ct));

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) =>
            ReadAsync(buffer.AsMemory(offset, count), ct).AsTask();

        private int Track(int n)
        {
            _read += n;
            if (n > 0) progress?.Invoke(length > 0 ? Math.Min(1, _read / (double)length) : 0);
            return n;
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
