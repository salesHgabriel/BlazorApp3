using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Teams;

/// <summary>Cabeçalho da página de detalhe do time.</summary>
public sealed record TeamSummaryQuery(int Id) : IQuery<TeamListItem?>;
