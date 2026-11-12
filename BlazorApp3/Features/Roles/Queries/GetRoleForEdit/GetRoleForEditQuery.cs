using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Roles;

public sealed record GetRoleForEditQuery(string Id) : IQuery<RoleEditModel?>;
