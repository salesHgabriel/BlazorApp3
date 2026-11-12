using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;

namespace BlazorApp3.Features.Teams;

public sealed record AddTeamMemberCommand(int TeamId, string UserId, TeamRole Role) : ICommand;
