using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Settings;

/// <summary>
/// Leitura das configurações do sistema com cache em memória (usadas a cada login, validação de cookie e job).
/// Salvar pela tela chama <see cref="Invalidate"/>.
/// </summary>
public sealed class SystemSettingsProvider(IServiceScopeFactory scopeFactory)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
    private SystemSettings? _cached;
    private DateTime _loadedAt;

    public async Task<SystemSettings> GetAsync(CancellationToken ct = default)
    {
        var cached = _cached;
        if (cached is not null && DateTime.UtcNow - _loadedAt < Ttl) return cached;

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var loaded = await db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1, ct) ?? new SystemSettings();
        _cached = loaded;
        _loadedAt = DateTime.UtcNow;
        return loaded;
    }

    public void Invalidate() => _cached = null;
}
