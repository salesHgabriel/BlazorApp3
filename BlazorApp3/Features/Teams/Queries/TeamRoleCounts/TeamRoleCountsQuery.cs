using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;

namespace BlazorApp3.Features.Teams;

/// <summary>Quantidade de membros por papel no time (proprietários, admins, membros).</summary>
public sealed record TeamRoleCountsQuery(int TeamId) : IQuery<Dictionary<TeamRole, int>>;
