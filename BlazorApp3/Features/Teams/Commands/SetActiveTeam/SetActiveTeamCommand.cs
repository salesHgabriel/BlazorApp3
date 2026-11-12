using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Teams;

/// <summary>Define o time ativo do usuário (null = branding da plataforma).</summary>
public sealed record SetActiveTeamCommand(string UserId, int? TeamId) : ICommand;
