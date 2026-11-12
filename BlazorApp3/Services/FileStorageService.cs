namespace BlazorApp3.Services;

public sealed record UploadedFile(string FileName, string Url, long Size, string ContentType);

/// <summary>Armazena arquivos em App_Data/uploads, servidos em /uploads.</summary>
public sealed class FileStorageService
{
    public const string RequestPath = "/uploads";

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".ico", ".pdf", ".txt", ".csv", ".xlsx", ".docx", ".zip"
    };

    public string RootPath { get; }

    public FileStorageService(IWebHostEnvironment env)
    {
        RootPath = Path.Combine(env.ContentRootPath, "App_Data", "uploads");
        Directory.CreateDirectory(RootPath);
    }

    public async Task<UploadedFile> SaveAsync(Stream input, string originalName, string contentType, string folder, long length,
        Action<double>? progress = null, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            throw new AppException($"Tipo de arquivo não permitido: {ext}");

        var safeFolder = string.Concat(folder.Where(c => char.IsLetterOrDigit(c) || c == '-')).ToLowerInvariant();
        if (safeFolder.Length == 0) safeFolder = "misc";
        var dir = Path.Combine(RootPath, safeFolder);
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(dir, fileName);

        var buffer = new byte[81920];
        long written = 0;
        try
        {
            await using var output = File.Create(fullPath);
            int read;
            while ((read = await input.ReadAsync(buffer, ct)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), ct);
                written += read;
                progress?.Invoke(length > 0 ? Math.Min(1, written / (double)length) : 0);
            }
        }
        catch
        {
            File.Delete(fullPath);
            throw;
        }

        return new UploadedFile(originalName, $"{RequestPath}/{safeFolder}/{fileName}", written, contentType);
    }
}
