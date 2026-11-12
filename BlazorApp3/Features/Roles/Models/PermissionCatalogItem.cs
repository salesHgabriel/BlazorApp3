using BlazorApp3.Services;

namespace BlazorApp3.Features.Roles;

/// <summary>Permissão fixa + papéis que a concedem.</summary>
public sealed class PermissionCatalogItem
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Group { get; init; } = "";
    public string Description { get; init; } = "";
    public List<Badge> Roles { get; set; } = [];
}
