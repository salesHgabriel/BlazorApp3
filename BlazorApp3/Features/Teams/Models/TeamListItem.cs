using BlazorApp3.Features.Users;

namespace BlazorApp3.Features.Teams;

public sealed class TeamListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";
    public string? Description { get; init; }
    public string? DisplayName { get; init; }
    public string? LogoUrl { get; init; }
    public string PrimaryColor { get; init; } = "";
    public string? AccentColor { get; init; }
    public string? SidebarColor { get; init; }
    public bool IsActive { get; init; }
    public bool UseCustomBranding { get; init; }
    public int MemberCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<UserLookup> TopMembers { get; set; } = [];
}
