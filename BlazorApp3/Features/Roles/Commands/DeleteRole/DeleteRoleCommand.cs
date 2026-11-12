using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Roles;

/// <summary>Exclui um papel (papéis de sistema são protegidos).</summary>
public sealed record DeleteRoleCommand(string Id) : ICommand, IInvalidatesPermissions;
