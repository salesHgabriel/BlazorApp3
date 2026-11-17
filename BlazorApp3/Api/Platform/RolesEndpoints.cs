using BlazorApp3.Application.Cqrs;
using BlazorApp3.Features.Roles;
using BlazorApp3.Security;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BlazorApp3.Api.Platform;

/// <summary>/api/roles e /api/permissions — papéis dinâmicos e catálogo de permissões fixas.</summary>
public static class RolesEndpoints
{
    public static void MapRolesEndpoints(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/roles").WithTags("Roles");

        g.MapGet("/", async (string? search, string? sortBy, bool? sortDesc, int? page, int? pageSize, IDispatcher d, CancellationToken ct) =>
            TypedResults.Ok(await d.QueryAsync(new SearchRolesQuery
            {
                Search = search,
                SortBy = sortBy ?? "name",
                SortDesc = sortDesc ?? false,
                Page = ApiEndpoints.ClampPage(page),
                PageSize = ApiEndpoints.ClampPageSize(pageSize)
            }, ct)))
            .RequirePermission(Perm.RolesView);

        g.MapGet("/options", async (IDispatcher d, CancellationToken ct) =>
            TypedResults.Ok(await d.QueryAsync(new RoleOptionsQuery(), ct)))
            .RequirePermission(Perm.RolesView, Perm.UsersView);

        g.MapGet("/{id}", async Task<Results<Ok<RoleEditModel>, NotFound>> (string id, IDispatcher d, CancellationToken ct) =>
            await d.QueryAsync(new GetRoleForEditQuery(id), ct) is { } role ? TypedResults.Ok(role) : TypedResults.NotFound())
            .RequirePermission(Perm.RolesView);

        g.MapPost("/", async (RoleEditModel model, IDispatcher d, CancellationToken ct) =>
        {
            model.Id = null;
            await d.SendAsync(new SaveRoleCommand(model), ct);
            return TypedResults.NoContent();
        })
            .RequirePermission(Perm.RolesCreate);

        g.MapPut("/{id}", async (string id, RoleEditModel model, IDispatcher d, CancellationToken ct) =>
        {
            model.Id = id;
            await d.SendAsync(new SaveRoleCommand(model), ct);
            return TypedResults.NoContent();
        })
            .RequirePermission(Perm.RolesEdit);

        g.MapDelete("/{id}", async (string id, IDispatcher d, CancellationToken ct) =>
        {
            await d.SendAsync(new DeleteRoleCommand(id), ct);
            return TypedResults.NoContent();
        })
            .RequirePermission(Perm.RolesDelete);
    }

    public static void MapPermissionsEndpoints(this RouteGroupBuilder api) =>
        api.MapGet("/permissions", async (IDispatcher d, CancellationToken ct) =>
                TypedResults.Ok(await d.QueryAsync(new PermissionCatalogQuery(), ct)))
            .WithTags("Permissions")
            .RequirePermission(Perm.PermissionsView, Perm.RolesView);
}
