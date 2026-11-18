namespace BlazorApp3.Services.Reports;

/// <summary>Seção "Reports" do appsettings.</summary>
public sealed class ReportsOptions
{
    public const string Section = "Reports";

    /// <summary>Acima deste número de registros o relatório é gerado em segundo plano e entregue por notificação/e-mail.</summary>
    public int BackgroundThreshold { get; set; } = 1000;
    /// <summary>Dias que o arquivo gerado fica disponível para download.</summary>
    public int RetentionDays { get; set; } = 7;
    /// <summary>Tamanho máximo (MB) para anexar ao e-mail; acima disso o e-mail leva só o link de download.</summary>
    public int MaxEmailAttachmentMb { get; set; } = 10;
    // Os arquivos vão para o IFileStorage (seção "Storage"), na chave "reports/...", com visibilidade privada.
}
