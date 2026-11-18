using System.Text.Json;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Features.Reports;
using BlazorApp3.Services.Reports;

namespace BlazorApp3.Features.Users;

/// <summary>Relatório de usuários em segundo plano: mesmos filtros/ordenação da tela (<see cref="SearchUsersQuery"/>).</summary>
internal sealed class UsersReportSource(IDispatcher dispatcher, ReportService reports) : IBackgroundReportSource
{
    public const string SourceKey = "users";
    public string Key => SourceKey;

    public async Task<RenderedReport> RenderAsync(BackgroundReportRequest request, CancellationToken ct)
    {
        var filters = JsonSerializer.Deserialize<SearchUsersQuery>(request.FiltersJson) ?? new SearchUsersQuery();
        var items = await dispatcher.QueryAsync(new ExportUsersQuery(filters), ct);
        var (name, type, content) = await reports.RenderAsync(items, UsersReport.Columns(), request.Options, request.GeneratedBy,
            request.FilterLabels.Select(f => (f.Label, f.Value)).ToList(), ct);
        return new RenderedReport(name, type, content, items.Count);
    }
}
