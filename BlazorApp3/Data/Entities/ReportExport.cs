using System.ComponentModel.DataAnnotations;

namespace BlazorApp3.Data;

/// <summary>
/// Relatório gerado em segundo plano (Hangfire) quando passa do limite de registros para download imediato.
/// O arquivo fica fora da pasta pública (App_Data/reports) e só o dono baixa, por <c>/reports/{Id}/download</c>.
/// </summary>
public class ReportExport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(450)] public string UserId { get; set; } = "";
    public AppUser? User { get; set; }

    /// <summary>Origem do relatório (ex.: "users"), resolvida por um <c>IBackgroundReportSource</c>.</summary>
    [MaxLength(40)] public string Source { get; set; } = "";
    [MaxLength(120)] public string Title { get; set; } = "";
    /// <summary>Excel | Pdf</summary>
    [MaxLength(10)] public string Format { get; set; } = "";
    public ReportExportStatus Status { get; set; } = ReportExportStatus.Pending;

    /// <summary>Registros estimados no momento do pedido (os filtros da tela).</summary>
    public int EstimatedRows { get; set; }
    public int? RowCount { get; set; }

    [MaxLength(200)] public string? FileName { get; set; }
    [MaxLength(100)] public string? ContentType { get; set; }
    /// <summary>Nome do arquivo dentro da pasta de relatórios (nunca um caminho vindo do usuário).</summary>
    [MaxLength(100)] public string? StorageName { get; set; }
    public long? SizeBytes { get; set; }
    [MaxLength(500)] public string? Error { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    /// <summary>Depois disso o arquivo é apagado pelo job de limpeza.</summary>
    public DateTime? ExpiresAt { get; set; }
}

public enum ReportExportStatus { Pending = 0, Processing = 1, Completed = 2, Failed = 3 }
