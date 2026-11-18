using BlazorApp3.Application.Cqrs;
using BlazorApp3.Features.Branding;
using BlazorApp3.Services.Files.Storage;

namespace BlazorApp3.Services.Reports;

/// <summary>
/// Gera relatórios Excel/PDF. A geração roda fora do thread do circuito (CPU) e usa a marca configurada (nome, cor, logo).
/// Para um relatório novo: defina as colunas (<see cref="ReportColumn{T}"/>), busque os itens e chame <see cref="RenderAsync{T}"/>.
/// </summary>
public sealed class ReportService(IDispatcher dispatcher, IFileStorage files, ILogger<ReportService> logger)
{
    public const int MaxRows = 50_000;

    public async Task<(string FileName, string ContentType, byte[] Content)> RenderAsync<T>(
        IReadOnlyList<T> items, IReadOnlyList<ReportColumn<T>> columns, ReportOptions options,
        string generatedBy, IReadOnlyList<(string Label, string Value)> filters, CancellationToken ct = default)
    {
        var branding = await dispatcher.QueryAsync(new GetBrandingQuery(), ct);
        var ctx = new ReportContext(branding.AppName, EffectiveBranding.Color(branding.PrimaryColor, "#4f46e5"),
            options.ShowLogo ? await LoadLogoAsync(branding.LogoUrl, ct) : null, generatedBy, filters);

        var doc = ReportBuilder.Build(items, columns, options, ctx);
        var bytes = await Task.Run(() => options.Format == ReportFormat.Excel ? ExcelReportWriter.Render(doc) : PdfReportWriter.Render(doc), ct);
        return (ReportBuilder.FileName(doc.Title, options.Format), ReportBuilder.ContentType(options.Format), bytes);
    }

    /// <summary>Logo enviado no whitelabel (/uploads/...). Só PNG/JPG/WEBP (formatos aceitos nos dois motores).</summary>
    private async Task<byte[]?> LoadLogoAsync(string? url, CancellationToken ct)
    {
        if (FileStorageService.KeyFromUrl(url) is not { } key) return null;
        var ext = Path.GetExtension(key).ToLowerInvariant();
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".webp")) return null;

        try
        {
            var file = await files.OpenReadAsync(key, ct);
            if (file is null) return null;
            await using var content = file.Content;
            using var ms = new MemoryStream();
            await content.CopyToAsync(ms, ct);
            return ms.ToArray();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Sem logo o relatório sai do mesmo jeito
            logger.LogWarning(ex, "Logo do relatório não pôde ser lido ({Key}).", key);
            return null;
        }
    }
}
