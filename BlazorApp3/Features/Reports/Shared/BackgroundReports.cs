using System.Text.Json;
using BlazorApp3.Data;
using BlazorApp3.Services.Files.Storage;
using BlazorApp3.Services.Reports;
using Hangfire;
using Microsoft.Extensions.Options;

namespace BlazorApp3.Features.Reports;

/// <summary>Filtro aplicado, já em texto (vai no cabeçalho do relatório).</summary>
public sealed record ReportFilterLabel(string Label, string Value);

/// <summary>Argumento do job do Hangfire (serializado em JSON): tudo que o job precisa para refazer a consulta e renderizar.</summary>
public sealed class BackgroundReportRequest
{
    public Guid ReportId { get; set; }
    /// <summary>Chave do <see cref="IBackgroundReportSource"/> (ex.: "users").</summary>
    public string Source { get; set; } = "";
    public string UserId { get; set; } = "";
    public string GeneratedBy { get; set; } = "";
    /// <summary>Filtros da tela serializados (o tipo é conhecido pela fonte).</summary>
    public string FiltersJson { get; set; } = "{}";
    public ReportOptions Options { get; set; } = new();
    public List<ReportFilterLabel> FilterLabels { get; set; } = [];

    /// <summary>Exibido no painel do Hangfire.</summary>
    public override string ToString() => $"{Options.Title} ({Options.Format})";
}

/// <summary>Arquivo pronto.</summary>
public sealed record RenderedReport(string FileName, string ContentType, byte[] Content, int RowCount);

/// <summary>
/// Fonte de um relatório gerado em segundo plano. Para habilitar outra tela: implemente (veja <c>UsersReportSource</c>),
/// registre com <c>services.AddScoped&lt;IBackgroundReportSource, MinhaFonte&gt;()</c> e chame
/// <see cref="BackgroundReports.EnqueueAsync{TFilters}"/> com a mesma <see cref="Key"/>.
/// </summary>
public interface IBackgroundReportSource
{
    string Key { get; }
    Task<RenderedReport> RenderAsync(BackgroundReportRequest request, CancellationToken ct);
}

/// <summary>Decide entre download imediato e segundo plano, e enfileira o job.</summary>
public sealed class BackgroundReports(AppDbContext db, IBackgroundJobClient jobs, IOptions<ReportsOptions> options)
{
    public int Threshold => options.Value.BackgroundThreshold;

    /// <summary>Relatórios com mais registros que o limite vão para segundo plano.</summary>
    public bool ShouldRunInBackground(int rowCount) => rowCount > Threshold;

    public async Task<Guid> EnqueueAsync<TFilters>(string source, TFilters filters, ReportOptions reportOptions,
        string userId, string generatedBy, IEnumerable<(string Label, string Value)> filterLabels, int estimatedRows, CancellationToken ct = default)
    {
        var title = string.IsNullOrWhiteSpace(reportOptions.Title) ? source : reportOptions.Title.Trim();
        var row = new ReportExport
        {
            UserId = userId, Source = source, Title = title.Length > 120 ? title[..120] : title,
            Format = reportOptions.Format.ToString(), EstimatedRows = estimatedRows
        };
        db.ReportExports.Add(row);
        await db.SaveChangesAsync(ct);

        var request = new BackgroundReportRequest
        {
            ReportId = row.Id, Source = source, UserId = userId, GeneratedBy = generatedBy,
            FiltersJson = JsonSerializer.Serialize(filters), Options = reportOptions,
            FilterLabels = filterLabels.Select(f => new ReportFilterLabel(f.Label, f.Value)).ToList()
        };
        // A cultura atual (idioma da tela) é capturada pelo Hangfire e restaurada no job: cabeçalhos e datas saem no mesmo idioma
        jobs.Enqueue<ReportJobs>(j => j.GenerateAsync(request, CancellationToken.None));
        return row.Id;
    }
}

/// <summary>
/// Arquivos dos relatórios no <see cref="IFileStorage"/> configurado (disco local ou nuvem), com a chave
/// <c>reports/{StorageName}</c> e visibilidade privada: só saem pelo endpoint de download, que confere o dono.
/// </summary>
public sealed class ReportStorage(IFileStorage storage)
{
    public const string Folder = "reports";

    /// <summary>Validade da URL direta (SAS/pré-assinada) quando o provedor gera uma; o link do e-mail passa sempre pelo app.</summary>
    public static readonly TimeSpan DownloadUrlLifetime = TimeSpan.FromMinutes(5);

    public static string Key(string storageName) => StorageKey.Normalize(Folder, Path.GetFileName(storageName));

    public Task<long> SaveAsync(string storageName, byte[] content, string contentType, CancellationToken ct = default)
    {
        using var ms = new MemoryStream(content, writable: false);
        return storage.SaveAsync(Key(storageName), ms, contentType, StorageVisibility.Private, ct);
    }

    public Task<bool> ExistsAsync(string storageName, CancellationToken ct = default) =>
        storage.ExistsAsync(Key(storageName), ct);

    public Task<StoredFileStream?> OpenReadAsync(string storageName, CancellationToken ct = default) =>
        storage.OpenReadAsync(Key(storageName), ct);

    public async Task<byte[]?> ReadAllBytesAsync(string storageName, CancellationToken ct = default)
    {
        var file = await OpenReadAsync(storageName, ct);
        if (file is null) return null;
        await using var content = file.Content;
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        return ms.ToArray();
    }

    public Task<string?> GetDownloadUrlAsync(string storageName, string fileName, CancellationToken ct = default) =>
        storage.GetDownloadUrlAsync(Key(storageName), DownloadUrlLifetime, fileName, ct);

    public Task DeleteAsync(string? storageName, CancellationToken ct = default) =>
        string.IsNullOrEmpty(storageName) ? Task.CompletedTask : storage.DeleteAsync(Key(storageName), ct);
}
