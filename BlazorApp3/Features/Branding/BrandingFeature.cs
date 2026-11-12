using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Branding;

public sealed record GetBrandingQuery : IQuery<BrandingSettings>;
public sealed record SaveBrandingCommand(BrandingSettings Settings) : ICommand;
public sealed record ResetBrandingCommand : ICommand<BrandingSettings>;

internal sealed class BrandingHandlers(AppDbContext db) :
    IQueryHandler<GetBrandingQuery, BrandingSettings>,
    ICommandHandler<SaveBrandingCommand, Unit>,
    ICommandHandler<ResetBrandingCommand, BrandingSettings>
{
    public async Task<BrandingSettings> HandleAsync(GetBrandingQuery query, CancellationToken ct) =>
        await db.BrandingSettings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == 1, ct) ?? BrandingSettings.CreateDefault();

    public async Task<Unit> HandleAsync(SaveBrandingCommand command, CancellationToken ct)
    {
        await SaveAsync(command.Settings, ct);
        return Unit.Value;
    }

    public async Task<BrandingSettings> HandleAsync(ResetBrandingCommand command, CancellationToken ct)
    {
        var defaults = BrandingSettings.CreateDefault();
        await SaveAsync(defaults, ct);
        return defaults;
    }

    private async Task SaveAsync(BrandingSettings settings, CancellationToken ct)
    {
        var copy = settings.Clone();
        copy.Id = 1;
        copy.UpdatedAt = DateTime.UtcNow;
        if (await db.BrandingSettings.AnyAsync(ct)) db.BrandingSettings.Update(copy);
        else db.BrandingSettings.Add(copy);
        await db.SaveChangesAsync(ct);
    }
}
