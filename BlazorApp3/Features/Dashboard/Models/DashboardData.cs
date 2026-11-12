using BlazorApp3.Features.Users;

namespace BlazorApp3.Features.Dashboard;

public sealed record DashboardData(
    int TotalUsers, int ActiveUsers, int NewUsers30d, int TotalTeams, int ActiveTeams, int TotalRoles,
    List<(string Label, int Count)> UsersByDepartment,
    List<(string Name, string Color, int Members)> TopTeams,
    List<UserLookup> RecentUsers);
