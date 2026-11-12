using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;

namespace BlazorApp3.Features.Branding;

internal sealed class ResetBrandingHandler(AppDbContext db) : ICommandHandler<ResetBrandingCommand, BrandingSettings>
{
    public async Task<BrandingSettings> HandleAsync(ResetBrandingCommand command, CancellationToken ct)
    {
        var defaults = BrandingSettings.CreateDefault();
        await BrandingStore.SaveAsync(db, defaults, ct);
        return defaults;
    }
}
