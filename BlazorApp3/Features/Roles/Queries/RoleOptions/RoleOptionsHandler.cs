using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Services;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Roles;

internal sealed class RoleOptionsHandler(AppDbContext db) : IQueryHandler<RoleOptionsQuery, List<SelectOption<string>>>
{
    public Task<List<SelectOption<string>>> HandleAsync(RoleOptionsQuery query, CancellationToken ct) =>
        db.Roles.OrderBy(r => r.Name).Select(r => new SelectOption<string>(r.Id, r.Name!, r.Color)).ToListAsync(ct);
}
