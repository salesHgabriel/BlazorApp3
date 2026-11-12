using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Teams;

/// <summary>Time ativo salvo no perfil do usuário (null = branding da plataforma).</summary>
public sealed record ActiveTeamIdQuery(string UserId) : IQuery<int?>;
