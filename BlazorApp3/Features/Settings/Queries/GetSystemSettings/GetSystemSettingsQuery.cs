using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;

namespace BlazorApp3.Features.Settings;

/// <summary>Configurações do sistema (cópia editável).</summary>
public sealed record GetSystemSettingsQuery : IQuery<SystemSettings>;

internal sealed class GetSystemSettingsHandler(SystemSettingsProvider provider) : IQueryHandler<GetSystemSettingsQuery, SystemSettings>
{
    public async Task<SystemSettings> HandleAsync(GetSystemSettingsQuery query, CancellationToken ct) =>
        (await provider.GetAsync(ct)).Clone();
}
