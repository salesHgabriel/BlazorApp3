using System.Security.Claims;
using BlazorApp3.Data;
using BlazorApp3.Features.Users;
using BlazorApp3.Services.Reports;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Reports;

public static class ReportsServiceCollectionExtensions
{
    /// <summary>Relatórios Excel/PDF + geração em segundo plano (acima de <c>Reports:BackgroundThreshold</c> registros).</summary>
    public static IServiceCollection AddReports(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ReportsOptions>(configuration.GetSection(ReportsOptions.Section));
        services.AddScoped<ReportService>();
        services.AddScoped<BackgroundReports>();
        services.AddSingleton<ReportStorage>();
        services.AddScoped<IBackgroundReportSource, UsersReportSource>();
        return services;
    }

    /// <summary>
    /// GET /reports/{id}/download: só o dono baixa (login por cookie; sem login vai para a tela de entrar e volta).
    /// Usado pelo link do e-mail e pela página "Meus relatórios".
    /// </summary>
    public static IEndpointRouteBuilder MapReportDownloads(this IEndpointRouteBuilder app)
    {
        app.MapGet("/reports/{id:guid}/download", async (Guid id, ClaimsPrincipal user, AppDbContext db, ReportStorage storage, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var report = await db.ReportExports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct);
            if (report is not { Status: ReportExportStatus.Completed, StorageName: not null } || report.ExpiresAt <= DateTime.UtcNow)
                return Results.NotFound();
            var fileName = report.FileName ?? report.StorageName;

            // Nuvem: depois de conferir o dono, redireciona para uma URL assinada de curta duração (o arquivo não passa pelo app)
            if (await storage.GetDownloadUrlAsync(report.StorageName, fileName, ct) is { } url) return Results.Redirect(url);

            var file = await storage.OpenReadAsync(report.StorageName, ct);
            return file is null
                ? Results.NotFound()
                : Results.Stream(file.Content, report.ContentType ?? file.ContentType, fileName);
        }).RequireAuthorization().ExcludeFromDescription();
        return app;
    }
}
