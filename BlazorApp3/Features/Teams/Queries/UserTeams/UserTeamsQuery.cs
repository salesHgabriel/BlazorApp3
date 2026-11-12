using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Teams;

/// <summary>Times ativos dos quais o usuário participa (menu "Meus times").</summary>
public sealed record UserTeamsQuery(string UserId) : IQuery<List<TeamOption>>;
