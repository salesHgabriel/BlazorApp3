using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Settings;

public sealed record SaveSystemSettingsCommand(SystemSettings Settings) : ICommand;

internal sealed class SaveSystemSettingsHandler(AppDbContext db, SystemSettingsProvider provider) : ICommandHandler<SaveSystemSettingsCommand, Unit>
{
    public async Task<Unit> HandleAsync(SaveSystemSettingsCommand command, CancellationToken ct)
    {
        var copy = command.Settings.Clone();
        copy.Id = 1;
        copy.UpdatedAt = DateTime.UtcNow;
        if (await db.SystemSettings.AnyAsync(ct)) db.SystemSettings.Update(copy);
        else db.SystemSettings.Add(copy);
        await db.SaveChangesAsync(ct);
        provider.Invalidate();
        return Unit.Value;
    }
}
