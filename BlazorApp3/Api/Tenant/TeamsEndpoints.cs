using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Features.Teams;
using BlazorApp3.Security;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BlazorApp3.Api.Tenant;

/// <summary>/api/teams — times, branding do time e membros.</summary>
public static class TeamsEndpoints
{
    public sealed record AddMemberRequest(string UserId, TeamRole Role = TeamRole.Member);
    public sealed record UpdateMemberRequest(TeamRole Role);

    public static void MapTeamsEndpoints(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/teams").WithTags("Teams");

        g.MapGet("/", async (string? search, bool? isActive, bool? customBranding, string? sortBy, int? page, int? pageSize, IDispatcher d, CancellationToken ct) =>
            TypedResults.Ok(await d.QueryAsync(new SearchTeamsQuery
            {
                Search = search,
                IsActive = isActive,
                CustomBranding = customBranding,
                SortBy = sortBy ?? "name",
                Page = ApiEndpoints.ClampPage(page),
                PageSize = ApiEndpoints.ClampPageSize(pageSize, 9)
            }, ct)))
            .RequirePermission(Perm.TeamsView);

        g.MapGet("/options", async (IDispatcher d, CancellationToken ct) =>
            TypedResults.Ok(await d.QueryAsync(new TeamOptionsQuery(), ct)))
            .RequirePermission(Perm.TeamsView, Perm.UsersView);

        g.MapGet("/{id:int}", async Task<Results<Ok<TeamEditModel>, NotFound>> (int id, IDispatcher d, CancellationToken ct) =>
            await d.QueryAsync(new GetTeamForEditQuery(id), ct) is { } team ? TypedResults.Ok(team) : TypedResults.NotFound())
            .RequirePermission(Perm.TeamsView);

        g.MapGet("/{id:int}/summary", async Task<Results<Ok<TeamListItem>, NotFound>> (int id, IDispatcher d, CancellationToken ct) =>
            await d.QueryAsync(new TeamSummaryQuery(id), ct) is { } team ? TypedResults.Ok(team) : TypedResults.NotFound())
            .RequirePermission(Perm.TeamsView);

        g.MapPost("/", async Task<Created<TeamEditModel>> (TeamEditModel model, IDispatcher d, CancellationToken ct) =>
        {
            model.Id = null;
            var id = await d.SendAsync(new SaveTeamCommand(model), ct);
            return TypedResults.Created($"{ApiEndpoints.Prefix}/teams/{id}", await d.QueryAsync(new GetTeamForEditQuery(id), ct));
        })
            .RequirePermission(Perm.TeamsCreate);

        g.MapPut("/{id:int}", async (int id, TeamEditModel model, IDispatcher d, CancellationToken ct) =>
        {
            model.Id = id;
            await d.SendAsync(new SaveTeamCommand(model), ct);
            return TypedResults.NoContent();
        })
            .RequirePermission(Perm.TeamsEdit);

        g.MapDelete("/{id:int}", async (int id, IDispatcher d, CancellationToken ct) =>
        {
            await d.SendAsync(new DeleteTeamCommand(id), ct);
            return TypedResults.NoContent();
        })
            .RequirePermission(Perm.TeamsDelete);

        // ---------------- Membros
        var members = g.MapGroup("/{id:int}/members");

        members.MapGet("/", async (int id, string? search, TeamRole? role, int? page, int? pageSize, IDispatcher d, CancellationToken ct) =>
            TypedResults.Ok(await d.QueryAsync(new TeamMembersQuery(id)
            {
                Search = search,
                Role = role,
                Page = ApiEndpoints.ClampPage(page),
                PageSize = ApiEndpoints.ClampPageSize(pageSize)
            }, ct)))
            .RequirePermission(Perm.TeamsView);

        members.MapGet("/role-counts", async (int id, IDispatcher d, CancellationToken ct) =>
            TypedResults.Ok(await d.QueryAsync(new TeamRoleCountsQuery(id), ct)))
            .RequirePermission(Perm.TeamsView);

        members.MapPost("/", async (int id, AddMemberRequest r, IDispatcher d, CancellationToken ct) =>
        {
            await d.SendAsync(new AddTeamMemberCommand(id, r.UserId, r.Role), ct);
            return TypedResults.NoContent();
        })
            .RequirePermission(Perm.TeamsManageMembers);

        members.MapPut("/{userId}", async (int id, string userId, UpdateMemberRequest r, IDispatcher d, CancellationToken ct) =>
        {
            await d.SendAsync(new UpdateTeamMemberRoleCommand(id, userId, r.Role), ct);
            return TypedResults.NoContent();
        })
            .RequirePermission(Perm.TeamsManageMembers);

        members.MapDelete("/{userId}", async (int id, string userId, IDispatcher d, CancellationToken ct) =>
        {
            await d.SendAsync(new RemoveTeamMemberCommand(id, userId), ct);
            return TypedResults.NoContent();
        })
            .RequirePermission(Perm.TeamsManageMembers);
    }
}
