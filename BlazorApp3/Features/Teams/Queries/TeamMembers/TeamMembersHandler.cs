using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Services;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Teams;

internal sealed class TeamMembersHandler(AppDbContext db) : IQueryHandler<TeamMembersQuery, PagedResult<TeamMemberItem>>
{
    public Task<PagedResult<TeamMemberItem>> HandleAsync(TeamMembersQuery query, CancellationToken ct)
    {
        var q = db.TeamMembers.AsNoTracking().Where(tm => tm.TeamId == query.TeamId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.LikePattern();
            q = q.Where(tm => EF.Functions.Like(tm.User.FullName, s, "\\") || EF.Functions.Like(tm.User.Email!, s, "\\"));
        }
        if (query.Role.HasValue) q = q.Where(tm => tm.Role == query.Role);

        return q.OrderByDescending(tm => tm.Role).ThenBy(tm => tm.User.FullName)
            .Select(tm => new TeamMemberItem
            {
                UserId = tm.UserId, FullName = tm.User.FullName, Email = tm.User.Email!, AvatarUrl = tm.User.AvatarUrl,
                JobTitle = tm.User.JobTitle, IsActive = tm.User.IsActive, Role = tm.Role, JoinedAt = tm.JoinedAt
            }).ToPagedAsync(query.Page, query.PageSize, ct);
    }
}
