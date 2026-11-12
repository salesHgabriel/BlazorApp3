using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Services;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Users;

internal sealed class LookupUsersHandler(AppDbContext db) : IQueryHandler<LookupUsersQuery, PagedResult<UserLookup>>
{
    public Task<PagedResult<UserLookup>> HandleAsync(LookupUsersQuery query, CancellationToken ct)
    {
        var q = db.Users.AsNoTracking().Where(u => u.IsActive);
        if (!string.IsNullOrWhiteSpace(query.Term))
        {
            var s = query.Term.LikePattern();
            q = q.Where(u => EF.Functions.Like(u.FullName, s, "\\") || EF.Functions.Like(u.Email!, s, "\\"));
        }
        if (query.ExcludeTeamId is { } teamId)
            q = q.Where(u => !db.TeamMembers.Any(tm => tm.TeamId == teamId && tm.UserId == u.Id));

        return q.OrderBy(u => u.FullName)
            .Select(u => new UserLookup(u.Id, u.FullName, u.Email!, u.AvatarUrl, u.Department))
            .ToPagedAsync(query.Page, query.PageSize, ct);
    }
}
