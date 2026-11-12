using BlazorApp3.Application.Cqrs;
using BlazorApp3.Services;

namespace BlazorApp3.Features.Users;

/// <summary>Busca lazy paginada de usuários ativos (autocomplete). Pode excluir os membros de um time.</summary>
public sealed record LookupUsersQuery(string? Term, int Page, int PageSize, int? ExcludeTeamId = null) : IQuery<PagedResult<UserLookup>>;
