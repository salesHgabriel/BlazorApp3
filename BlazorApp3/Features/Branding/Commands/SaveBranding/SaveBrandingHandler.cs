using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;

namespace BlazorApp3.Features.Branding;

internal sealed class SaveBrandingHandler(AppDbContext db) : ICommandHandler<SaveBrandingCommand, Unit>
{
    public async Task<Unit> HandleAsync(SaveBrandingCommand command, CancellationToken ct)
    {
        await BrandingStore.SaveAsync(db, command.Settings, ct);
        return Unit.Value;
    }
}
