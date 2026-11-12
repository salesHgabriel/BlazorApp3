using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Roles;

/// <summary>Cria (Id nulo) ou atualiza um papel e sua lista de permissões.</summary>
public sealed record SaveRoleCommand(RoleEditModel Model) : ICommand, IInvalidatesPermissions;
