using BlazorApp3.Data;

namespace BlazorApp3.Features.Teams;

/// <summary>Projeção de Team → TeamListItem, reutilizada pela busca e pelo resumo.</summary>
internal static class TeamProjections
{
    public static IQueryable<TeamListItem> ToListItems(this IQueryable<Team> teams) => teams.Select(t => new TeamListItem
    {
        Id = t.Id, Name = t.Name, Slug = t.Slug, Description = t.Description, DisplayName = t.DisplayName, LogoUrl = t.LogoUrl,
        PrimaryColor = t.PrimaryColor, AccentColor = t.AccentColor, SidebarColor = t.SidebarColor, IsActive = t.IsActive,
        UseCustomBranding = t.UseCustomBranding, CreatedAt = t.CreatedAt, MemberCount = t.Members.Count
    });
}
