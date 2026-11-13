using System.Globalization;
using BlazorApp3.Localization;
using ClosedXML.Excel;

namespace BlazorApp3.Services.Reports;

/// <summary>
/// Relatório .xlsx com ClosedXML (OpenXML puro, sem Excel/COM instalado: roda em Windows, Linux, macOS e containers).
/// Datas e números são gravados tipados (o usuário consegue filtrar/somar), cabeçalho congelado e autofiltro.
/// </summary>
public static class ExcelReportWriter
{
    public static byte[] Render(ReportDocument doc)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(SheetName(doc.Title));
        var primary = SafeColor(doc.PrimaryColor);
        var cols = Math.Max(1, doc.Columns.Count);
        var row = 1;

        // ---- Cabeçalho do relatório
        Title(ws.Range(row, 1, row, cols), doc.Title, 16, bold: true, primary);
        row++;
        if (doc.Subtitle is not null) { Title(ws.Range(row, 1, row, cols), doc.Subtitle, 11, bold: false, XLColor.FromHtml("#4b5563")); row++; }
        Title(ws.Range(row, 1, row, cols),
            AppText.Format("{0} · Gerado por {1} em {2}", doc.AppName, doc.GeneratedBy, doc.GeneratedAt.ToString("g")), 9, bold: false, XLColor.Gray);
        row++;
        foreach (var (label, value) in doc.Filters)
        {
            Title(ws.Range(row, 1, row, cols), $"{label}: {value}", 9, bold: false, XLColor.FromHtml("#4b5563"));
            row++;
        }
        row++;

        // ---- Cabeçalho da tabela
        var headerRow = row;
        for (var c = 0; c < doc.Columns.Count; c++)
        {
            var cell = ws.Cell(row, c + 1);
            cell.Value = doc.Columns[c].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.FromHtml(Contrast(doc.PrimaryColor));
            cell.Style.Fill.BackgroundColor = primary;
            cell.Style.Alignment.Horizontal = Align(doc.Columns[c].Align);
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        ws.Row(row).Height = 22;
        row++;

        // ---- Linhas (com cabeçalho de grupo, se houver)
        var grouped = doc.Groups.Any(g => g.Key is not null);
        var zebra = XLColor.FromHtml("#f6f7fb");
        foreach (var group in doc.Groups)
        {
            if (group.Key is not null)
            {
                var gr = ws.Range(row, 1, row, cols).Merge();
                gr.Value = $"{group.Key} ({group.Rows.Count})";
                gr.Style.Font.Bold = true;
                gr.Style.Fill.BackgroundColor = XLColor.FromHtml("#e8eaf3");
                row++;
            }
            for (var i = 0; i < group.Rows.Count; i++)
            {
                var values = group.Rows[i];
                for (var c = 0; c < doc.Columns.Count; c++)
                {
                    var cell = ws.Cell(row, c + 1);
                    SetValue(cell, values[c], doc.Columns[c]);
                    cell.Style.Alignment.Horizontal = Align(doc.Columns[c].Align);
                }
                if (doc.Options.Zebra && i % 2 == 1) ws.Range(row, 1, row, cols).Style.Fill.BackgroundColor = zebra;
                row++;
            }
        }
        var lastRow = Math.Max(headerRow, row - 1);

        var table = ws.Range(headerRow, 1, lastRow, cols);
        table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        table.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        table.Style.Border.OutsideBorderColor = XLColor.FromHtml("#d1d5db");
        table.Style.Border.InsideBorderColor = XLColor.FromHtml("#e5e7eb");
        if (!grouped && lastRow > headerRow) table.SetAutoFilter();
        ws.SheetView.FreezeRows(headerRow);

        // ---- Resumo
        if (doc.Options.IncludeSummary)
        {
            row++;
            var total = ws.Cell(row, 1);
            total.Value = AppText.Format("Total: {0} registro(s)", doc.RowCount);
            total.Style.Font.Bold = true;
        }

        // ---- Larguras e impressão
        for (var c = 1; c <= cols; c++)
        {
            var column = ws.Column(c);
            column.AdjustToContents(headerRow, lastRow);
            column.Width = Math.Clamp(column.Width + 2, 8, 60);
        }
        ws.PageSetup.PageOrientation = doc.Options.Landscape ? XLPageOrientation.Landscape : XLPageOrientation.Portrait;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.SetRowsToRepeatAtTop(headerRow, headerRow);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void Title(IXLRange range, string text, double size, bool bold, XLColor color)
    {
        range.Merge();
        range.Value = text;
        range.Style.Font.FontSize = size;
        range.Style.Font.Bold = bold;
        range.Style.Font.FontColor = color;
    }

    private static void SetValue(IXLCell cell, object? value, ReportColumnDef col)
    {
        switch (value)
        {
            case null: break;
            case string s: cell.Value = s; break;
            case bool b: cell.Value = b ? AppText.Get("Sim") : AppText.Get("Não"); break;
            case DateTime d:
                cell.Value = d;
                cell.Style.DateFormat.Format = ExcelDateFormat(col.Format);
                break;
            case DateTimeOffset d:
                cell.Value = d.LocalDateTime;
                cell.Style.DateFormat.Format = ExcelDateFormat(col.Format);
                break;
            case int or long or short: cell.Value = Convert.ToInt64(value); break;
            case decimal or double or float:
                cell.Value = Convert.ToDouble(value);
                if (col.Format is not null) cell.Style.NumberFormat.Format = col.Format.StartsWith('C') ? "#,##0.00" : "#,##0.##";
                break;
            case IEnumerable<string> list: cell.Value = string.Join(", ", list); break;
            default: cell.Value = value.ToString(); break;
        }
    }

    /// <summary>Formato de data do Excel conforme a cultura atual ("d" = só data).</summary>
    private static string ExcelDateFormat(string? format)
    {
        var dmy = !CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.StartsWith("M", StringComparison.Ordinal);
        var date = dmy ? "dd/mm/yyyy" : "mm/dd/yyyy";
        return format == "d" ? date : date + " hh:mm";
    }

    private static XLAlignmentHorizontalValues Align(ReportAlign a) => a switch
    {
        ReportAlign.Center => XLAlignmentHorizontalValues.Center,
        ReportAlign.Right => XLAlignmentHorizontalValues.Right,
        _ => XLAlignmentHorizontalValues.Left
    };

    private static string SheetName(string title)
    {
        var name = new string(title.Where(ch => !"[]:*?/\\".Contains(ch)).ToArray()).Trim();
        if (name.Length == 0) name = "Relatório";
        return name.Length > 31 ? name[..31] : name;
    }

    private static XLColor SafeColor(string hex)
    {
        try { return XLColor.FromHtml(hex); } catch { return XLColor.FromHtml("#4f46e5"); }
    }

    private static string Contrast(string hex) => EffectiveBranding.Contrast(EffectiveBranding.Color(hex, "#4f46e5"));
}
