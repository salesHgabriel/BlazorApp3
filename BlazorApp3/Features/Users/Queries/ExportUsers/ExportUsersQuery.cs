using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Localization;
using BlazorApp3.Services;
using BlazorApp3.Services.Reports;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Users;

/// <summary>Todos os usuários que atendem aos filtros da listagem (sem paginação), para relatórios.</summary>
public sealed record ExportUsersQuery(SearchUsersQuery Filters) : IQuery<List<UserListItem>>;

internal sealed class ExportUsersHandler(AppDbContext db) : IQueryHandler<ExportUsersQuery, List<UserListItem>>
{
    public async Task<List<UserListItem>> HandleAsync(ExportUsersQuery query, CancellationToken ct)
    {
        var search = new SearchUsersHandler(db); // mesmos filtros/ordenação da tela
        var q = SearchUsersHandler.Sort(search.Filter(db.Users.AsNoTracking(), query.Filters), query.Filters);
        if (await q.CountAsync(ct) > ReportService.MaxRows)
            throw new AppException(AppText.Format("O relatório tem mais de {0} registros. Refine os filtros.", ReportService.MaxRows));

        var items = await q.Select(SearchUsersHandler.ToListItem).ToListAsync(ct);
        await search.LoadBadgesAsync(items, ct);
        return items;
    }
}

/// <summary>Colunas disponíveis no relatório de usuários (o usuário escolhe quais e a ordem).</summary>
public static class UsersReport
{
    public static IReadOnlyList<ReportColumn<UserListItem>> Columns() =>
    [
        new("name", AppText.Get("Nome"), u => u.FullName, Width: 2f),
        new("email", AppText.Get("E-mail"), u => u.Email, Width: 2.2f),
        new("jobTitle", AppText.Get("Cargo"), u => u.JobTitle, Width: 1.5f),
        new("department", AppText.Get("Departamento"), u => u.Department, Width: 1.4f),
        new("roles", AppText.Get("Papéis"), u => u.Roles.Select(r => r.Label).ToList(), Width: 1.6f),
        new("teams", AppText.Get("Times"), u => u.Teams.Select(t => t.Label).ToList(), Width: 1.6f),
        new("status", AppText.Get("Status"), Status, Width: 1.3f),
        new("emailConfirmed", AppText.Get("E-mail confirmado"), u => u.EmailConfirmed, DefaultSelected: false, Align: ReportAlign.Center, Width: 1.3f),
        new("mfa", AppText.Get("MFA"), u => u.TwoFactorEnabled, DefaultSelected: false, Align: ReportAlign.Center, Width: 0.7f),
        new("created", AppText.Get("Cadastro"), u => u.CreatedAt.ToLocalTime(), Align: ReportAlign.Center, Width: 1.25f, Format: "d"),
        new("lastLogin", AppText.Get("Último acesso"), u => u.LastLoginAt?.ToLocalTime(), Align: ReportAlign.Center, Width: 1.6f),
        new("blockReason", AppText.Get("Motivo do bloqueio"), u => u.IsAdminBlock ? u.BlockReason : null, DefaultSelected: false, Width: 1.6f)
    ];

    /// <summary>Colunas que fazem sentido para agrupar.</summary>
    public static readonly string[] Groupable = ["department", "jobTitle", "status"];

    public static string Status(UserListItem u) =>
        u.IsBlocked ? AppText.Get("Bloqueado") :
        !u.IsActive ? AppText.Get("Inativo") :
        !u.EmailConfirmed ? AppText.Get("E-mail pendente") : AppText.Get("Ativo");
}
