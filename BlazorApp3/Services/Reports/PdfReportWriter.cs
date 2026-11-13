using BlazorApp3.Localization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BlazorApp3.Services.Reports;

/// <summary>
/// Relatório .pdf com QuestPDF (motor próprio com binários nativos para Windows, Linux e macOS, x64/arm64;
/// fonte padrão embutida, então funciona mesmo em containers sem fontes instaladas).
/// Cabeçalho com marca (logo/cor), filtros aplicados, tabela com cabeçalho repetido em cada página, grupos e rodapé paginado.
/// </summary>
public static class PdfReportWriter
{
    private const string Muted = "#6b7280";
    private const string Border = "#e5e7eb";

    public static byte[] Render(ReportDocument doc) => Create(doc).GeneratePdf();

    /// <summary>Documento QuestPDF (permite também gerar imagens das páginas, ex.: pré-visualização).</summary>
    public static IDocument Create(ReportDocument doc)
    {
        var primary = EffectiveBranding.Color(doc.PrimaryColor, "#4f46e5");
        var onPrimary = EffectiveBranding.Contrast(primary);
        var size = doc.Options.PageSize switch { "Letter" => PageSizes.Letter, "Legal" => PageSizes.Legal, _ => PageSizes.A4 };
        if (doc.Options.Landscape) size = size.Landscape();

        return Document.Create(container => container.Page(page =>
            {
                page.Size(size);
                page.Margin(28);
                page.PageColor(Colors.White);
                // Muitas colunas: fonte menor para caber sem quebrar palavras
                var fontSize = doc.Columns.Count switch { > 10 => 7.5f, > 7 => 8.5f, _ => 9.5f };
                page.DefaultTextStyle(t => t.FontSize(fontSize).FontColor("#1f2937"));

                page.Header().Element(h => Header(h, doc, primary));
                page.Content().PaddingTop(10).Element(c => Content(c, doc, primary, onPrimary));
                page.Footer().PaddingTop(6).Row(r =>
                {
                    r.RelativeItem().Text(doc.AppName).FontSize(8).FontColor(Muted);
                    r.RelativeItem().AlignRight().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(8).FontColor(Muted));
                        t.Span(AppText.Get("Página") + " ");
                        t.CurrentPageNumber();
                        t.Span(" / ");
                        t.TotalPages();
                    });
                });
            }))
            .WithMetadata(new DocumentMetadata { Title = doc.Title, Author = doc.GeneratedBy, Creator = doc.AppName });
    }

    private static void Header(IContainer container, ReportDocument doc, string primary) => container.Column(col =>
    {
        col.Item().Row(row =>
        {
            if (doc.Logo is { } logo)
                row.ConstantItem(72).Height(36).AlignMiddle().Image(logo).FitArea();

            row.RelativeItem().PaddingLeft(doc.Logo is null ? 0 : 10).AlignMiddle().Column(t =>
            {
                t.Item().Text(doc.Title).FontSize(16).Bold().FontColor(primary);
                if (doc.Subtitle is not null) t.Item().Text(doc.Subtitle).FontSize(10).FontColor("#4b5563");
            });

            row.ConstantItem(190).AlignMiddle().Column(m =>
            {
                m.Item().AlignRight().Text(AppText.Format("Gerado por {0}", doc.GeneratedBy)).FontSize(8).FontColor(Muted);
                m.Item().AlignRight().Text(doc.GeneratedAt.ToString("g")).FontSize(8).FontColor(Muted);
            });
        });

        if (doc.Filters.Count > 0)
        {
            col.Item().PaddingTop(6).Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(8).FontColor("#4b5563"));
                foreach (var (label, value) in doc.Filters)
                {
                    t.Span(label + ": ").SemiBold();
                    t.Span(value + "    ");
                }
            });
        }

        col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(primary);
    });

    private static void Content(IContainer container, ReportDocument doc, string primary, string onPrimary) => container.Column(col =>
    {
        if (doc.Columns.Count > 0)
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    foreach (var c in doc.Columns) cd.RelativeColumn(c.Width);
                });

                table.Header(h =>
                {
                    foreach (var c in doc.Columns)
                        h.Cell().Background(primary).PaddingVertical(5).PaddingHorizontal(4).Aligned(c.Align)
                            .Text(c.Header).FontColor(onPrimary).Bold();
                });

                foreach (var group in doc.Groups)
                {
                    if (group.Key is not null)
                    {
                        table.Cell().ColumnSpan((uint)doc.Columns.Count).Background("#e8eaf3").PaddingVertical(4).PaddingHorizontal(4)
                            .Text($"{group.Key} ({group.Rows.Count})").Bold();
                    }
                    for (var i = 0; i < group.Rows.Count; i++)
                    {
                        var bg = doc.Options.Zebra && i % 2 == 1 ? "#f6f7fb" : "#ffffff";
                        var values = group.Rows[i];
                        for (var c = 0; c < doc.Columns.Count; c++)
                        {
                            var column = doc.Columns[c];
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Border).PaddingVertical(4).PaddingHorizontal(4)
                                .Aligned(column.Align).Text(ReportBuilder.Text(values[c], column.Format));
                        }
                    }
                }
            });
        }

        if (doc.RowCount == 0)
            col.Item().PaddingTop(12).AlignCenter().Text(AppText.Get("Nenhum registro encontrado.")).FontColor(Muted);

        if (doc.Options.IncludeSummary)
            col.Item().PaddingTop(8).AlignRight().Text(AppText.Format("Total: {0} registro(s)", doc.RowCount)).Bold();
    });

    private static IContainer Aligned(this IContainer c, ReportAlign align) => align switch
    {
        ReportAlign.Center => c.AlignCenter(),
        ReportAlign.Right => c.AlignRight(),
        _ => c.AlignLeft()
    };
}
