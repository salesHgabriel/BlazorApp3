using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;

namespace BlazorApp3.Features.Teams;

/// <summary>Membros do time, paginados, com busca e filtro por papel no time.</summary>
public sealed class TeamMembersQuery(int teamId) : PagedQuery<TeamMemberItem>
{
    public int TeamId { get; } = teamId;
    public string? Search { get; set; }
    public TeamRole? Role { get; set; }
}
