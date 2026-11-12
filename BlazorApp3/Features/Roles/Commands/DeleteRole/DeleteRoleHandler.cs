using BlazorApp3.Localization;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Services;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp3.Features.Roles;

internal sealed class DeleteRoleHandler(RoleManager<AppRole> roles) : ICommandHandler<DeleteRoleCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeleteRoleCommand command, CancellationToken ct)
    {
        var role = await roles.FindByIdAsync(command.Id) ?? throw new AppException(AppText.Get("Papel não encontrado."));
        if (role.IsSystem) throw new AppException(AppText.Get("Papéis de sistema não podem ser excluídos."));
        AppException.ThrowIfFailed(await roles.DeleteAsync(role));
        return Unit.Value;
    }
}
