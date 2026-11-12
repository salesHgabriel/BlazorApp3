using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Branding;

/// <summary>Persiste a configuração única (Id = 1) de whitelabel. Usado por salvar e restaurar.</summary>
internal static class BrandingStore
{
    public static async Task SaveAsync(AppDbContext db, BrandingSettings settings, CancellationToken ct)
    {
        var copy = settings.Clone();
        copy.Id = 1;
        copy.UpdatedAt = DateTime.UtcNow;
        if (await db.BrandingSettings.AnyAsync(ct)) db.BrandingSettings.Update(copy);
        else db.BrandingSettings.Add(copy);
        await db.SaveChangesAsync(ct);
    }
}
