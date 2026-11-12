using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Roles;

/// <summary>Catálogo das permissões fixas com os papéis que concedem cada uma.</summary>
public sealed record PermissionCatalogQuery : IQuery<List<PermissionCatalogItem>>;
