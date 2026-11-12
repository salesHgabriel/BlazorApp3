namespace BlazorApp3.Features.Roles;

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
