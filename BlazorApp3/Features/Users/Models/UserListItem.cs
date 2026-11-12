using BlazorApp3.Services;

namespace BlazorApp3.Features.Users;

/// <summary>Linha da listagem de usuários (com papéis e times já resolvidos).</summary>
public sealed class UserListItem
{
    public string Id { get; init; } = "";
    public string FullName { get; init; } = "";
    public string Email { get; init; } = "";
    public string? JobTitle { get; init; }
    public string? Department { get; init; }
    public string? AvatarUrl { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public List<Badge> Roles { get; set; } = [];
    public List<Badge> Teams { get; set; } = [];
}
