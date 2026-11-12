using System.ComponentModel.DataAnnotations;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Security;
using BlazorApp3.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Roles;

// ------------------------------------------------------------------ Mensagens
public sealed class SearchRolesQuery : PagedQuery<RoleListItem>
{
    public string? Search { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDesc { get; set; }
}

public sealed record RoleOptionsQuery : IQuery<List<SelectOption<string>>>;
public sealed record GetRoleForEditQuery(string Id) : IQuery<RoleEditModel?>;
public sealed record PermissionCatalogQuery : IQuery<List<PermissionCatalogItem>>;

/// <summary>Permissões efetivas do usuário (união das roles). Usada pelo PermissionService com cache.</summary>
public sealed record UserPermissionsQuery(string UserId) : IQuery<HashSet<string>>;

public sealed record SaveRoleCommand(RoleEditModel Model) : ICommand, IInvalidatesPermissions;
public sealed record DeleteRoleCommand(string Id) : ICommand, IInvalidatesPermissions;

// ------------------------------------------------------------------ DTOs
public sealed class RoleListItem
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public string Color { get; init; } = "";
    public bool IsSystem { get; init; }
    public int UserCount { get; init; }
    public int PermissionCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class RoleEditModel
{
    public string? Id { get; set; }
    public bool IsSystem { get; set; }

    [Required(ErrorMessage = "Informe o nome do papel"), StringLength(60, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 60 caracteres")]
    public string Name { get; set; } = "";

    [StringLength(300)] public string? Description { get; set; }

    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Cor inválida")]
    public string Color { get; set; } = "#6366f1";

    public HashSet<int> PermissionIds { get; set; } = [];
}

public sealed class PermissionCatalogItem
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Group { get; init; } = "";
    public string Description { get; init; } = "";
    public List<Badge> Roles { get; set; } = [];
}

// ------------------------------------------------------------------ Handlers
internal sealed class RoleQueryHandlers(AppDbContext db) :
    IQueryHandler<SearchRolesQuery, PagedResult<RoleListItem>>,
    IQueryHandler<RoleOptionsQuery, List<SelectOption<string>>>,
    IQueryHandler<GetRoleForEditQuery, RoleEditModel?>,
    IQueryHandler<PermissionCatalogQuery, List<PermissionCatalogItem>>,
    IQueryHandler<UserPermissionsQuery, HashSet<string>>
{
    public Task<PagedResult<RoleListItem>> HandleAsync(SearchRolesQuery query, CancellationToken ct)
    {
        var q = db.Roles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.LikePattern();
            q = q.Where(r => EF.Functions.Like(r.Name!, s, "\\") || EF.Functions.Like(r.Description!, s, "\\"));
        }

        var projected = q.Select(r => new RoleListItem
        {
            Id = r.Id, Name = r.Name!, Description = r.Description, Color = r.Color, IsSystem = r.IsSystem, CreatedAt = r.CreatedAt,
            UserCount = db.UserRoles.Count(ur => ur.RoleId == r.Id),
            PermissionCount = r.Permissions.Count
        });

        projected = (query.SortBy, query.SortDesc) switch
        {
            ("users", false) => projected.OrderBy(r => r.UserCount),
            ("users", true) => projected.OrderByDescending(r => r.UserCount),
            ("permissions", false) => projected.OrderBy(r => r.PermissionCount),
            ("permissions", true) => projected.OrderByDescending(r => r.PermissionCount),
            ("created", false) => projected.OrderBy(r => r.CreatedAt),
            ("created", true) => projected.OrderByDescending(r => r.CreatedAt),
            (_, true) => projected.OrderByDescending(r => r.Name),
            _ => projected.OrderByDescending(r => r.IsSystem).ThenBy(r => r.Name)
        };
        return projected.ToPagedAsync(query.Page, query.PageSize, ct);
    }

    public Task<List<SelectOption<string>>> HandleAsync(RoleOptionsQuery query, CancellationToken ct) =>
        db.Roles.OrderBy(r => r.Name).Select(r => new SelectOption<string>(r.Id, r.Name!, r.Color)).ToListAsync(ct);

    public async Task<RoleEditModel?> HandleAsync(GetRoleForEditQuery query, CancellationToken ct)
    {
        var role = await db.Roles.AsNoTracking().Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == query.Id, ct);
        return role is null ? null : new RoleEditModel
        {
            Id = role.Id, Name = role.Name!, Description = role.Description, Color = role.Color, IsSystem = role.IsSystem,
            PermissionIds = role.Permissions.Select(p => p.PermissionId).ToHashSet()
        };
    }

    public async Task<List<PermissionCatalogItem>> HandleAsync(PermissionCatalogQuery query, CancellationToken ct)
    {
        var items = await db.Permissions.AsNoTracking().OrderBy(p => p.Id)
            .Select(p => new PermissionCatalogItem { Id = p.Id, Name = p.Name, Group = p.Group, Description = p.Description })
            .ToListAsync(ct);
        var links = await db.RolePermissions.Select(rp => new { rp.PermissionId, rp.Role.Name, rp.Role.Color }).ToListAsync(ct);
        foreach (var item in items)
            item.Roles = links.Where(l => l.PermissionId == item.Id).Select(l => new Badge(l.Name!, l.Color)).OrderBy(b => b.Label).ToList();
        return items;
    }

    public async Task<HashSet<string>> HandleAsync(UserPermissionsQuery query, CancellationToken ct)
    {
        var active = await db.Users.Where(u => u.Id == query.UserId).Select(u => u.IsActive).FirstOrDefaultAsync(ct);
        if (!active) return [];

        var names = await (
            from ur in db.UserRoles
            join rp in db.RolePermissions on ur.RoleId equals rp.RoleId
            join p in db.Permissions on rp.PermissionId equals p.Id
            where ur.UserId == query.UserId
            select p.Name).Distinct().ToListAsync(ct);
        return names.ToHashSet(StringComparer.Ordinal);
    }
}

internal sealed class RoleCommandHandlers(RoleManager<AppRole> roles, AppDbContext db) :
    ICommandHandler<SaveRoleCommand, Unit>,
    ICommandHandler<DeleteRoleCommand, Unit>
{
    public async Task<Unit> HandleAsync(SaveRoleCommand command, CancellationToken ct)
    {
        var m = command.Model;
        AppRole role;
        if (m.Id is null)
        {
            role = new AppRole(m.Name.Trim()) { Description = m.Description, Color = m.Color, CreatedAt = DateTime.UtcNow };
            AppException.ThrowIfFailed(await roles.CreateAsync(role));
        }
        else
        {
            role = await roles.FindByIdAsync(m.Id) ?? throw new AppException("Papel não encontrado.");
            if (role.IsSystem && !string.Equals(role.Name, m.Name.Trim(), StringComparison.Ordinal))
                throw new AppException("Papéis de sistema não podem ser renomeados.");
            role.Name = m.Name.Trim();
            role.Description = m.Description;
            role.Color = m.Color;
            AppException.ThrowIfFailed(await roles.UpdateAsync(role));
        }

        // Papel de sistema sempre mantém todas as permissões
        var wanted = role.IsSystem ? Perm.All.Select(p => p.Id).ToHashSet() : m.PermissionIds;
        await db.RolePermissions.Where(rp => rp.RoleId == role.Id).ExecuteDeleteAsync(ct);
        db.RolePermissions.AddRange(wanted.Select(pid => new RolePermission { RoleId = role.Id, PermissionId = pid }));
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> HandleAsync(DeleteRoleCommand command, CancellationToken ct)
    {
        var role = await roles.FindByIdAsync(command.Id) ?? throw new AppException("Papel não encontrado.");
        if (role.IsSystem) throw new AppException("Papéis de sistema não podem ser excluídos.");
        AppException.ThrowIfFailed(await roles.DeleteAsync(role));
        return Unit.Value;
    }
}
