namespace BlazorApp3.Services.Reports;

public enum ReportFormat { Excel, Pdf }
public enum ReportAlign { Left, Center, Right }

/// <summary>Coluna disponível num relatório (o usuário escolhe quais e em que ordem).</summary>
/// <param name="Width">Largura relativa (PDF) / aproximada em caracteres (Excel).</param>
public sealed record ReportColumn<T>(string Key, string Header, Func<T, object?> Value,
    bool DefaultSelected = true, ReportAlign Align = ReportAlign.Left, float Width = 1f, string? Format = null);

/// <summary>Escolhas do usuário no dialog "Gerar relatório".</summary>
public sealed class ReportOptions
{
    public ReportFormat Format { get; set; } = ReportFormat.Excel;
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    /// <summary>Colunas na ordem escolhida.</summary>
    public List<string> Columns { get; set; } = [];
    /// <summary>Agrupar por esta coluna (null = sem agrupamento).</summary>
    public string? GroupBy { get; set; }
    public bool IncludeFilters { get; set; } = true;
    public bool IncludeSummary { get; set; } = true;
    public bool Zebra { get; set; } = true;
    // ---- PDF
    public bool Landscape { get; set; } = true;
    /// <summary>A4 | Letter | Legal</summary>
    public string PageSize { get; set; } = "A4";
    public bool ShowLogo { get; set; } = true;

    public ReportOptions Clone() => new()
    {
        Format = Format, Title = Title, Subtitle = Subtitle, Columns = [.. Columns], GroupBy = GroupBy,
        IncludeFilters = IncludeFilters, IncludeSummary = IncludeSummary, Zebra = Zebra,
        Landscape = Landscape, PageSize = PageSize, ShowLogo = ShowLogo
    };
}

/// <summary>Coluna já resolvida (sem o seletor genérico) usada pelos renderizadores.</summary>
public sealed record ReportColumnDef(string Key, string Header, ReportAlign Align, float Width, string? Format);

/// <summary>Documento pronto para renderizar (independente do formato).</summary>
public sealed class ReportDocument
{
    public string Title { get; init; } = "";
    public string? Subtitle { get; init; }
    public string AppName { get; init; } = "";
    public string PrimaryColor { get; init; } = "#4f46e5";
    public byte[]? Logo { get; init; }
    public string GeneratedBy { get; init; } = "";
    public DateTime GeneratedAt { get; init; } = DateTime.Now;
    public IReadOnlyList<(string Label, string Value)> Filters { get; init; } = [];
    public IReadOnlyList<ReportColumnDef> Columns { get; init; } = [];
    /// <summary>Linhas agrupadas (um único grupo com Key null quando não há agrupamento).</summary>
    public IReadOnlyList<ReportGroup> Groups { get; init; } = [];
    public ReportOptions Options { get; init; } = new();

    public int RowCount => Groups.Sum(g => g.Rows.Count);
}

public sealed record ReportGroup(string? Key, IReadOnlyList<object?[]> Rows);

/// <summary>Contexto comum (marca, quem gerou, filtros aplicados).</summary>
public sealed record ReportContext(string AppName, string PrimaryColor, byte[]? Logo, string GeneratedBy,
    IReadOnlyList<(string Label, string Value)> Filters);

public static class ReportBuilder
{
    /// <summary>Monta o documento a partir dos itens, das colunas disponíveis e das escolhas do usuário.</summary>
    public static ReportDocument Build<T>(IEnumerable<T> items, IReadOnlyList<ReportColumn<T>> available, ReportOptions options, ReportContext ctx)
    {
        var byKey = available.ToDictionary(c => c.Key);
        var cols = options.Columns.Where(byKey.ContainsKey).Select(k => byKey[k]).ToList();
        if (cols.Count == 0) cols = available.Where(c => c.DefaultSelected).ToList();

        var group = options.GroupBy is { } g && byKey.TryGetValue(g, out var gc) ? gc : null;
        if (group is not null) cols.Remove(group); // a coluna do grupo vira o cabeçalho do grupo

        object?[] Row(T item) => cols.Select(c => c.Value(item)).ToArray();

        IReadOnlyList<ReportGroup> groups = group is null
            ? [new ReportGroup(null, items.Select(Row).ToList())]
            : items.GroupBy(i => Text(group.Value(i), group.Format))
                .OrderBy(x => string.IsNullOrEmpty(x.Key) ? 1 : 0).ThenBy(x => x.Key, StringComparer.CurrentCultureIgnoreCase)
                .Select(x => new ReportGroup(string.IsNullOrEmpty(x.Key) ? "—" : x.Key, x.Select(Row).ToList()))
                .ToList();

        return new ReportDocument
        {
            Title = string.IsNullOrWhiteSpace(options.Title) ? ctx.AppName : options.Title.Trim(),
            Subtitle = string.IsNullOrWhiteSpace(options.Subtitle) ? null : options.Subtitle.Trim(),
            AppName = ctx.AppName, PrimaryColor = ctx.PrimaryColor, Logo = options.ShowLogo ? ctx.Logo : null,
            GeneratedBy = ctx.GeneratedBy, GeneratedAt = DateTime.Now,
            Filters = options.IncludeFilters ? ctx.Filters : [],
            Columns = cols.Select(c => new ReportColumnDef(c.Key, c.Header, c.Align, c.Width, c.Format)).ToList(),
            Groups = groups,
            Options = options
        };
    }

    /// <summary>Texto de uma célula (PDF, agrupamento). O Excel grava datas/números tipados.</summary>
    public static string Text(object? value, string? format = null) => value switch
    {
        null => "",
        string s => s,
        bool b => b ? Localization.AppText.Get("Sim") : Localization.AppText.Get("Não"),
        DateTime d => d.ToString(format ?? "g"),
        DateTimeOffset d => d.LocalDateTime.ToString(format ?? "g"),
        IFormattable f => f.ToString(format, null),
        IEnumerable<string> list => string.Join(", ", list),
        _ => value.ToString() ?? ""
    };

    public static string FileName(string title, ReportFormat format)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(title.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        if (safe.Length == 0) safe = "relatorio";
        return $"{safe} {DateTime.Now:yyyy-MM-dd HHmm}.{(format == ReportFormat.Excel ? "xlsx" : "pdf")}";
    }

    public static string ContentType(ReportFormat format) => format == ReportFormat.Excel
        ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        : "application/pdf";
}
