using System.Security.Claims;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Features.Users;
using BlazorApp3.Security;
using BlazorApp3.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace BlazorApp3.Api.Platform;

/// <summary>/api/users — CRUD e ações em lote de usuários.</summary>
public static class UsersEndpoints
{
    public sealed record SearchUsersRequest(
        string? Search, string[]? RoleIds, int[]? TeamIds, string[]? Departments, string? Status,
        DateTime? CreatedFrom, DateTime? CreatedTo, string? SortBy, bool? SortDesc, int? Page, int? PageSize);

    public sealed record BlockUsersRequest(IReadOnlyList<string> Ids, string? Reason, int? Hours);

    public static void MapUsersEndpoints(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/users").WithTags("Users");

        g.MapGet("/", async ([AsParameters] SearchUsersRequest r, IDispatcher d, CancellationToken ct) =>
            TypedResults.Ok(await d.QueryAsync(new SearchUsersQuery
            {
                Search = r.Search,
                RoleIds = r.RoleIds?.ToList() ?? [],
                TeamIds = r.TeamIds?.ToList() ?? [],
                Departments = r.Departments?.ToList() ?? [],
                Status = r.Status,
                CreatedFrom = r.CreatedFrom,
                CreatedTo = r.CreatedTo,
                SortBy = r.SortBy ?? "name",
                SortDesc = r.SortDesc ?? false,
                Page = ApiEndpoints.ClampPage(r.Page),
                PageSize = ApiEndpoints.ClampPageSize(r.PageSize)
            }, ct)))
            .RequirePermission(Perm.UsersView);

        g.MapGet("/lookup", async (string? term, int? page, int? pageSize, int? excludeTeamId, IDispatcher d, CancellationToken ct) =>
            TypedResults.Ok(await d.QueryAsync(new LookupUsersQuery(term, ApiEndpoints.ClampPage(page), ApiEndpoints.ClampPageSize(pageSize, 20), excludeTeamId), ct)))
            .RequirePermission(Perm.UsersView, Perm.TeamsManageMembers);

        g.MapGet("/departments", async (IDispatcher d, CancellationToken ct) =>
            TypedResults.Ok(await d.QueryAsync(new GetDepartmentsQuery(), ct)))
            .RequirePermission(Perm.UsersView);

        g.MapGet("/{id}", async Task<Results<Ok<UserEditModel>, NotFound>> (string id, IDispatcher d, CancellationToken ct) =>
            await d.QueryAsync(new GetUserForEditQuery(id), ct) is { } user ? TypedResults.Ok(Sanitize(user)) : TypedResults.NotFound())
            .RequirePermission(Perm.UsersView);

        g.MapPost("/", async Task<Results<Created<UserEditModel>, ProblemHttpResult>> (UserEditModel model, IDispatcher d, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(model.Password))
                return TypedResults.Problem("A senha é obrigatória na criação.", statusCode: StatusCodes.Status400BadRequest);
            model.Id = null;
            var id = await d.SendAsync(new CreateUserCommand(model), ct);
            var created = await d.QueryAsync(new GetUserForEditQuery(id), ct);
            return TypedResults.Created($"{ApiEndpoints.Prefix}/users/{id}", Sanitize(created!));
        })
            .RequirePermission(Perm.UsersCreate);

        g.MapPut("/{id}", async (string id, UserEditModel model, IDispatcher d, CancellationToken ct) =>
        {
            model.Id = id;
            await d.SendAsync(new UpdateUserCommand(model), ct);
            return TypedResults.NoContent();
        })
            .RequirePermission(Perm.UsersEdit);

        g.MapDelete("/{id}", async (string id, ClaimsPrincipal me, IDispatcher d, CancellationToken ct) =>
        {
            EnsureNotSelf([id], me);
            await d.SendAsync(new DeleteUsersCommand([id]), ct);
            return TypedResults.NoContent();
        })
            .RequirePermission(Perm.UsersDelete);

        g.MapPost("/{id}/resend-confirmation", async (string id, IDispatcher d, CancellationToken ct) =>
        {
            await d.SendAsync(new ResendConfirmationCommand(id), ct);
            return TypedResults.Accepted((string?)null);
        })
            .RequirePermission(Perm.UsersEdit);

        // ---------------- Ações em lote
        var batch = g.MapGroup("/batch");

        batch.MapPost("/delete", async (IdsRequest r, ClaimsPrincipal me, IDispatcher d, CancellationToken ct) =>
        {
            EnsureNotSelf(r.Ids, me);
            await d.SendAsync(new DeleteUsersCommand(r.Ids), ct);
            return TypedResults.NoContent();
        })
            .RequirePermission(Perm.UsersDelete);

        batch.MapPost("/activate", async (IdsRequest r, IDispatcher d, CancellationToken ct) =>
        {
            await d.SendAsync(new SetUsersActiveCommand(r.Ids, true), ct);
            return TypedResults.NoContent();
        })
            .RequirePermission(Perm.UsersEdit);

        batch.MapPost("/deactivate", async (IdsRequest r, ClaimsPrincipal me, IDispatcher d, CancellationToken ct) =>
        {
            EnsureNotSelf(r.Ids, me);
            await d.SendAsync(new SetUsersActiveCommand(r.Ids, false), ct);
            return TypedResults.NoContent();
        })
            .RequirePermission(Perm.UsersEdit);

        batch.MapPost("/block", async (BlockUsersRequest r, ClaimsPrincipal me, IDispatcher d, CancellationToken ct) =>
        {
            EnsureNotSelf(r.Ids, me);
            await d.SendAsync(new BlockUsersCommand(r.Ids, r.Reason, r.Hours, me.DisplayName()), ct);
            return TypedResults.NoContent();
        })
            .RequirePermission(Perm.UsersBlock);

        batch.MapPost("/unblock", async (IdsRequest r, IDispatcher d, CancellationToken ct) =>
        {
            await d.SendAsync(new UnblockUsersCommand(r.Ids), ct);
            return TypedResults.NoContent();
        })
            .RequirePermission(Perm.UsersBlock);
    }

    /// <summary>Nunca devolve senha na resposta.</summary>
    private static UserEditModel Sanitize(UserEditModel model)
    {
        model.Password = null;
        return model;
    }

    /// <summary>Evita que o administrador se exclua, inative ou bloqueie pela API.</summary>
    private static void EnsureNotSelf(IReadOnlyList<string> ids, ClaimsPrincipal me)
    {
        if (ids.Contains(me.UserId()))
            throw new AppException("Você não pode executar esta ação na sua própria conta.");
    }
}
