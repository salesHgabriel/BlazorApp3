using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Teams;

public sealed record DeleteTeamCommand(int Id) : ICommand;
