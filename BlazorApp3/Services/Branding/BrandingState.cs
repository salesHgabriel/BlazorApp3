using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Features.Branding;
using BlazorApp3.Features.Teams;

namespace BlazorApp3.Services;

/// <summary>
/// Estado do branding por circuito (usuário conectado): whitelabel global + time ativo.
/// Notifica a UI quando algo muda para aplicar o tema em tempo real.
/// </summary>
public sealed class BrandingState(IDispatcher dispatcher)
{
    private string? _userId;
    private bool _loaded;

    public BrandingSettings Global { get; private set; } = BrandingSettings.CreateDefault();
    public IReadOnlyList<TeamOption> MyTeams { get; private set; } = [];
    public TeamOption? ActiveTeam { get; private set; }
    public EffectiveBranding Effective { get; private set; } = EffectiveBranding.From(BrandingSettings.CreateDefault(), null);

    public event Action? Changed;

    public async Task InitializeAsync(string? userId)
    {
        if (_loaded && userId == _userId) return;
        _userId = userId;
        await ReloadAsync();
    }

    public async Task ReloadAsync()
    {
        Global = await dispatcher.QueryAsync(new GetBrandingQuery());
        if (_userId is not null)
        {
            MyTeams = await dispatcher.QueryAsync(new UserTeamsQuery(_userId));
            var activeId = await dispatcher.QueryAsync(new ActiveTeamIdQuery(_userId));
            ActiveTeam = MyTeams.FirstOrDefault(t => t.Id == activeId);
        }
        _loaded = true;
        Apply();
    }

    public async Task SetActiveTeamAsync(int? teamId)
    {
        if (_userId is null) return;
        await dispatcher.SendAsync(new SetActiveTeamCommand(_userId, teamId));
        ActiveTeam = MyTeams.FirstOrDefault(t => t.Id == teamId);
        Apply();
    }

    private void Apply()
    {
        Effective = EffectiveBranding.From(Global, ActiveTeam);
        Changed?.Invoke();
    }
}
