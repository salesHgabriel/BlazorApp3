using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Users;

internal sealed class GetDepartmentsHandler(AppDbContext db) : IQueryHandler<GetDepartmentsQuery, List<string>>
{
    public Task<List<string>> HandleAsync(GetDepartmentsQuery query, CancellationToken ct) =>
        db.Users.Where(u => u.Department != null && u.Department != "")
            .Select(u => u.Department!).Distinct().OrderBy(d => d).ToListAsync(ct);
}
