using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Roles;

public sealed class SearchRolesQuery : PagedQuery<RoleListItem>
{
    public string? Search { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDesc { get; set; }
}
