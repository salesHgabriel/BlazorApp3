using BlazorApp3.Localization;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Security;
using BlazorApp3.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Roles;

internal sealed class SaveRoleHandler(RoleManager<AppRole> roles, AppDbContext db) : ICommandHandler<SaveRoleCommand, Unit>
{
    public async Task<Unit> HandleAsync(SaveRoleCommand command, CancellationToken ct)
    {
        var m = command.Model;
        var role = m.Id is null ? await CreateAsync(m) : await UpdateAsync(m);

        // Papel de sistema sempre mantém todas as permissões
        var wanted = role.IsSystem ? Perm.All.Select(p => p.Id).ToHashSet() : m.PermissionIds;
        await db.RolePermissions.Where(rp => rp.RoleId == role.Id).ExecuteDeleteAsync(ct);
        db.RolePermissions.AddRange(wanted.Select(pid => new RolePermission { RoleId = role.Id, PermissionId = pid }));
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    private async Task<AppRole> CreateAsync(RoleEditModel m)
    {
        var role = new AppRole(m.Name.Trim()) { Description = m.Description, Color = m.Color, CreatedAt = DateTime.UtcNow };
        AppException.ThrowIfFailed(await roles.CreateAsync(role));
        return role;
    }

    private async Task<AppRole> UpdateAsync(RoleEditModel m)
    {
        var role = await roles.FindByIdAsync(m.Id!) ?? throw new AppException(AppText.Get("Papel não encontrado."));
        if (role.IsSystem && !string.Equals(role.Name, m.Name.Trim(), StringComparison.Ordinal))
            throw new AppException(AppText.Get("Papéis de sistema não podem ser renomeados."));
        role.Name = m.Name.Trim();
        role.Description = m.Description;
        role.Color = m.Color;
        AppException.ThrowIfFailed(await roles.UpdateAsync(role));
        return role;
    }
}
