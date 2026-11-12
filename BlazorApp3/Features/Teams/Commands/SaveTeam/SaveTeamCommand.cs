using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Teams;

/// <summary>Cria (Id nulo) ou atualiza um time. Retorna o Id.</summary>
public sealed record SaveTeamCommand(TeamEditModel Model) : ICommand<int>;
