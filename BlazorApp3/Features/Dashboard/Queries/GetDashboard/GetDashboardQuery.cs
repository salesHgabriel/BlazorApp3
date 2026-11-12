using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Dashboard;

public sealed record GetDashboardQuery : IQuery<DashboardData>;
