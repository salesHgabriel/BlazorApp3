using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Features.Users;
using BlazorApp3.Services;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Teams;

internal sealed class SearchTeamsHandler(AppDbContext db) : IQueryHandler<SearchTeamsQuery, PagedResult<TeamListItem>>
{
    public async Task<PagedResult<TeamListItem>> HandleAsync(SearchTeamsQuery query, CancellationToken ct)
    {
        var q = db.Teams.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.LikePattern();
            q = q.Where(t => EF.Functions.Like(t.Name, s, "\\") || EF.Functions.Like(t.DisplayName!, s, "\\") || EF.Functions.Like(t.Description!, s, "\\"));
        }
        if (query.IsActive.HasValue) q = q.Where(t => t.IsActive == query.IsActive);
        if (query.CustomBranding.HasValue) q = q.Where(t => t.UseCustomBranding == query.CustomBranding);

        var projected = q.ToListItems();
        projected = query.SortBy switch
        {
            "members" => projected.OrderByDescending(t => t.MemberCount),
            "created" => projected.OrderByDescending(t => t.CreatedAt),
            _ => projected.OrderBy(t => t.Name)
        };
        var page = await projected.ToPagedAsync(query.Page, query.PageSize, ct);

        await LoadTopMembersAsync(page.Items, ct);
        return page;
    }

    /// <summary>Até 5 membros por time (donos/admins primeiro) para a pilha de avatares do card.</summary>
    private async Task LoadTopMembersAsync(IReadOnlyList<TeamListItem> teams, CancellationToken ct)
    {
        var ids = teams.Select(t => t.Id).ToList();
        var members = await db.TeamMembers.Where(tm => ids.Contains(tm.TeamId))
            .OrderByDescending(tm => tm.Role).ThenBy(tm => tm.JoinedAt)
            .Select(tm => new { tm.TeamId, User = new UserLookup(tm.UserId, tm.User.FullName, tm.User.Email!, tm.User.AvatarUrl, tm.User.Department) })
            .ToListAsync(ct);
        foreach (var t in teams)
            t.TopMembers = members.Where(m => m.TeamId == t.Id).Take(5).Select(m => m.User).ToList();
    }
}
