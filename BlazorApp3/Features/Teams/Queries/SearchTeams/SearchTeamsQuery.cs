using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Teams;

public sealed class SearchTeamsQuery : PagedQuery<TeamListItem>
{
    public SearchTeamsQuery() => PageSize = 9;

    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public bool? CustomBranding { get; set; }
    public string SortBy { get; set; } = "name";
}
