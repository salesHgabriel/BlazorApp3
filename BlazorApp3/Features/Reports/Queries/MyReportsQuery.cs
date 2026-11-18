using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Reports;

public sealed record ReportExportItem(Guid Id, string Title, string Format, ReportExportStatus Status, int EstimatedRows, int? RowCount,
    string? FileName, long? SizeBytes, string? Error, DateTime CreatedAt, DateTime? CompletedAt, DateTime? ExpiresAt)
{
    public bool InProgress => Status is ReportExportStatus.Pending or ReportExportStatus.Processing;
    public bool Downloadable => Status == ReportExportStatus.Completed && ExpiresAt > DateTime.UtcNow;
}

/// <summary>Relatórios gerados em segundo plano pelo usuário (mais recentes primeiro).</summary>
public sealed record MyReportsQuery(string UserId, int Take = 50) : IQuery<List<ReportExportItem>>;

internal sealed class MyReportsHandler(AppDbContext db) : IQueryHandler<MyReportsQuery, List<ReportExportItem>>
{
    public Task<List<ReportExportItem>> HandleAsync(MyReportsQuery query, CancellationToken ct) =>
        db.ReportExports.AsNoTracking()
            .Where(r => r.UserId == query.UserId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(query.Take)
            .Select(r => new ReportExportItem(r.Id, r.Title, r.Format, r.Status, r.EstimatedRows, r.RowCount, r.FileName, r.SizeBytes,
                r.Error, r.CreatedAt, r.CompletedAt, r.ExpiresAt))
            .ToListAsync(ct);
}

/// <summary>Remove um relatório do usuário (registro + arquivo).</summary>
public sealed record DeleteReportCommand(string UserId, Guid Id) : ICommand;

internal sealed class DeleteReportHandler(AppDbContext db, ReportStorage storage) : ICommandHandler<DeleteReportCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeleteReportCommand command, CancellationToken ct)
    {
        var report = await db.ReportExports.FirstOrDefaultAsync(r => r.Id == command.Id && r.UserId == command.UserId, ct);
        if (report is null) return Unit.Value;
        await storage.DeleteAsync(report.StorageName, ct);
        db.ReportExports.Remove(report);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
