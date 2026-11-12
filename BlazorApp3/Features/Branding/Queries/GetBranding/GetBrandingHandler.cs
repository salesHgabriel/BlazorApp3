using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Branding;

internal sealed class GetBrandingHandler(AppDbContext db) : IQueryHandler<GetBrandingQuery, BrandingSettings>
{
    public async Task<BrandingSettings> HandleAsync(GetBrandingQuery query, CancellationToken ct) =>
        await db.BrandingSettings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == 1, ct) ?? BrandingSettings.CreateDefault();
}
