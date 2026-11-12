using BlazorApp3.Data;

namespace BlazorApp3.Features.Teams;

public sealed class TeamMemberItem
{
    public string UserId { get; init; } = "";
    public string FullName { get; init; } = "";
    public string Email { get; init; } = "";
    public string? AvatarUrl { get; init; }
    public string? JobTitle { get; init; }
    public bool IsActive { get; init; }
    public TeamRole Role { get; init; }
    public DateTime JoinedAt { get; init; }
}
