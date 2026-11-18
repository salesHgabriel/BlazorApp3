using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Features.Branding;
using BlazorApp3.Features.Notifications;
using BlazorApp3.Features.Settings;
using BlazorApp3.Localization;
using BlazorApp3.Services;
using BlazorApp3.Services.Email;
using BlazorApp3.Services.Reports;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BlazorApp3.Features.Reports;

/// <summary>
/// Relatórios em segundo plano (Hangfire). <see cref="GenerateAsync"/> refaz a consulta, renderiza, grava o arquivo em
/// App_Data/reports e avisa o usuário: notificação no sino (tempo real) e, se o e-mail estiver configurado, um e-mail com o
/// arquivo anexado (ou só o link, se passar do tamanho máximo).
/// </summary>
public sealed class ReportJobs(
    AppDbContext db,
    IEnumerable<IBackgroundReportSource> sources,
    ReportStorage storage,
    INotifier notifier,
    IBackgroundJobClient jobs,
    SystemSettingsProvider settings,
    IMailSender mail,
    IDispatcher dispatcher,
    AppUrls urls,
    IOptions<ReportsOptions> reportOptions,
    IOptions<EmailOptions> emailOptions,
    IWebHostEnvironment env,
    ILogger<ReportJobs> logger)
{
    public const string PurgeExpiredId = "relatorios-remover-expirados";

    public static string DownloadPath(Guid id) => $"reports/{id}/download";

    /// <summary>Sem novas tentativas automáticas: uma falha vira notificação para o usuário pedir de novo.</summary>
    [JobDisplayName("Relatório: {0}")]
    [AutomaticRetry(Attempts = 0)]
    public async Task GenerateAsync(BackgroundReportRequest request, CancellationToken ct)
    {
        var report = await db.ReportExports.FirstOrDefaultAsync(r => r.Id == request.ReportId, ct);
        if (report is null || report.Status == ReportExportStatus.Completed) return; // removido pelo usuário ou já processado

        report.Status = ReportExportStatus.Processing;
        await db.SaveChangesAsync(ct);

        try
        {
            var source = sources.FirstOrDefault(s => s.Key == request.Source)
                ?? throw new InvalidOperationException($"Fonte de relatório '{request.Source}' não registrada.");
            var file = await source.RenderAsync(request, ct);

            var storageName = $"{report.Id:N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
            await storage.SaveAsync(storageName, file.Content, file.ContentType, ct);

            report.Status = ReportExportStatus.Completed;
            report.FileName = file.FileName;
            report.ContentType = file.ContentType;
            report.StorageName = storageName;
            report.SizeBytes = file.Content.LongLength;
            report.RowCount = file.RowCount;
            report.CompletedAt = DateTime.UtcNow;
            report.ExpiresAt = report.CompletedAt.Value.AddDays(Math.Max(1, reportOptions.Value.RetentionDays));
            report.Error = null;
            await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // servidor desligando: o Hangfire reenfileira o job
        }
        catch (Exception ex)
        {
            if (ex is not AppException) logger.LogError(ex, "Falha ao gerar o relatório {ReportId} ({Source}).", report.Id, request.Source);
            report.Status = ReportExportStatus.Failed;
            report.Error = Cut(ex is AppException ? ex.Message : AppText.Get("Erro inesperado ao gerar o arquivo."), 500);
            report.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);

            notifier.Publish(new NotificationRequest
            {
                UserIds = [report.UserId], Email = false,
                Title = AppText.Get("Falha ao gerar relatório"),
                Message = $"{report.Title}: {report.Error}",
                Icon = "bi-file-earmark-x", Level = NotificationLevel.Danger, Link = "reports"
            });
            return;
        }

        notifier.Publish(new NotificationRequest
        {
            UserIds = [report.UserId], Email = false,
            Title = AppText.Get("Relatório pronto"),
            Message = AppText.Format("{0}: {1} registro(s). Disponível para download até {2}.",
                report.Title, report.RowCount ?? 0, report.ExpiresAt!.Value.ToLocalTime().ToString("g")),
            Icon = "bi-file-earmark-check", Level = NotificationLevel.Success, Link = "reports"
        });

        if (await EmailEnabledAsync())
            jobs.Enqueue<ReportJobs>(j => j.SendEmailAsync(report.Id));
    }

    /// <summary>E-mail com o relatório anexado (ou o link, se o arquivo passar de <c>Reports:MaxEmailAttachmentMb</c>).</summary>
    [JobDisplayName("E-mail do relatório {0}")]
    [Queue(JobQueues.Emails)]
    [AutomaticRetry(Attempts = 5)]
    public async Task SendEmailAsync(Guid reportId)
    {
        var report = await db.ReportExports.AsNoTracking().Include(r => r.User).FirstOrDefaultAsync(r => r.Id == reportId);
        if (report is not { Status: ReportExportStatus.Completed, StorageName: not null, User.Email: not null }) return;

        var user = report.User;
        var brand = await dispatcher.QueryAsync(new GetBrandingQuery());
        var maxBytes = Math.Max(0, reportOptions.Value.MaxEmailAttachmentMb) * 1024L * 1024L;
        // Só baixa do armazenamento quando vai anexar (na nuvem, isso é uma leitura do blob/objeto)
        var bytes = report.SizeBytes is { } size && size <= maxBytes ? await storage.ReadAllBytesAsync(report.StorageName) : null;
        var attach = bytes is not null;
        if (!attach && !await storage.ExistsAsync(report.StorageName)) return; // arquivo removido: não manda link quebrado

        var paragraphs = new List<string>
        {
            AppText.Format("Olá, {0}!", EmailTemplate.FirstName(user.FullName)),
            AppText.Format("O relatório \"{0}\" ficou pronto: {1} registro(s).", report.Title, report.RowCount ?? 0),
            attach
                ? AppText.Get("O arquivo está anexado a este e-mail. Você também pode baixá-lo pelo sistema.")
                : AppText.Get("O arquivo é grande demais para ir anexado. Baixe-o pelo botão abaixo.")
        };
        var content = new EmailTemplate.Content(AppText.Get("Relatório pronto"), paragraphs)
        {
            ButtonText = AppText.Get("Baixar relatório"),
            ButtonUrl = urls.Absolute(DownloadPath(report.Id)),
            Footnote = AppText.Format("O link fica disponível até {0}.", report.ExpiresAt?.ToLocalTime().ToString("g") ?? "")
        };
        IReadOnlyList<EmailAttachment> attachments = attach
            ? [new EmailAttachment(report.FileName ?? report.StorageName, report.ContentType ?? "application/octet-stream", bytes!)]
            : [];

        var subject = AppText.Format("Relatório pronto: {0}", report.Title);
        await mail.SendAsync(user.Email!, user.FullName, subject, EmailTemplate.Render(brand, content), attachments, brand.AppName);
    }

    /// <summary>Diário: apaga arquivos vencidos e pedidos com falha antigos.</summary>
    [JobDisplayName("Remover relatórios expirados")]
    [DisableConcurrentExecution(60)]
    [AutomaticRetry(Attempts = 3)]
    public async Task<int> PurgeExpiredAsync()
    {
        var now = DateTime.UtcNow;
        var oldFailed = now.AddDays(-Math.Max(1, reportOptions.Value.RetentionDays));
        var stale = await db.ReportExports
            .Where(r => (r.ExpiresAt != null && r.ExpiresAt < now) || (r.Status == ReportExportStatus.Failed && r.CreatedAt < oldFailed))
            .ToListAsync();
        foreach (var r in stale)
        {
            try { await storage.DeleteAsync(r.StorageName); }
            catch (Exception ex) { logger.LogWarning(ex, "Arquivo do relatório {ReportId} não pôde ser apagado.", r.Id); continue; }
            db.ReportExports.Remove(r);
        }
        await db.SaveChangesAsync();
        return stale.Count;
    }

    /// <summary>
    /// E-mail "configurado": cópia por e-mail ligada nas configurações do sistema e um provedor que entrega de verdade
    /// (em Development também vale o Pickup, que grava o .eml em App_Data/emails).
    /// </summary>
    private async Task<bool> EmailEnabledAsync()
    {
        var s = await settings.GetAsync();
        return s.EmailNotificationsEnabled && (emailOptions.Value.IsDeliveryConfigured() || env.IsDevelopment());
    }

    private static string Cut(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
}
