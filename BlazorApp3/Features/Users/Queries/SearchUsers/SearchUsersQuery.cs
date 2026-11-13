using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Users;

/// <summary>Listagem paginada de usuários com busca, multi filtros e ordenação.</summary>
public sealed class SearchUsersQuery : PagedQuery<UserListItem>
{
    public string? Search { get; set; }
    public List<string> RoleIds { get; set; } = [];
    public List<int> TeamIds { get; set; } = [];
    public List<string> Departments { get; set; } = [];
    /// <summary>active | inactive | blocked | pending (e-mail não confirmado) | null = todos</summary>
    public string? Status { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDesc { get; set; }

    public int ActiveFilterCount =>
        (RoleIds.Count > 0 ? 1 : 0) + (TeamIds.Count > 0 ? 1 : 0) + (Departments.Count > 0 ? 1 : 0) +
        (string.IsNullOrEmpty(Status) ? 0 : 1) + (CreatedFrom.HasValue ? 1 : 0) + (CreatedTo.HasValue ? 1 : 0);
}
