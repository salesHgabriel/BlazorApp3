using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Users;

internal sealed class UsersRangeHandler(AppDbContext db) : IQueryHandler<UsersRangeQuery, UsersRange>
{
    public async Task<UsersRange> HandleAsync(UsersRangeQuery query, CancellationToken ct)
    {
        var q = db.Users.AsNoTracking().OrderBy(u => u.FullName);
        var items = await q.Skip(query.Skip).Take(query.Take)
            .Select(u => new UserLookup(u.Id, u.FullName, u.Email!, u.AvatarUrl, u.Department)).ToListAsync(ct);
        return new UsersRange(items, await q.CountAsync(ct));
    }
}
