using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Roles;

/// <summary>Permissões efetivas do usuário (união das roles). Usada pelo PermissionService com cache.</summary>
public sealed record UserPermissionsQuery(string UserId) : IQuery<HashSet<string>>;
