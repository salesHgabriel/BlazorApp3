using System.Security.Claims;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Features.Notifications;
using BlazorApp3.Features.Roles;
using BlazorApp3.Features.Teams;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BlazorApp3.Api.Tenant;

/// <summary>/api/me — dados do usuário logado: perfil, permissões, times e notificações. Só exige login.</summary>
public static class MeEndpoints
{
    public sealed record MeResponse(string Id, string Name, string? Email, int? ActiveTeamId, IReadOnlyCollection<string> Permissions);
    public sealed record ActiveTeamRequest(int? TeamId);

    public static void MapMeEndpoints(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/me").WithTags("Me");

        g.MapGet("/", async (ClaimsPrincipal me, IDispatcher d, CancellationToken ct) =>
        {
            var id = me.UserId();
            var permissions = await d.QueryAsync(new UserPermissionsQuery(id), ct);
            var activeTeam = await d.QueryAsync(new ActiveTeamIdQuery(id), ct);
            return TypedResults.Ok(new MeResponse(id, me.DisplayName(), me.FindFirstValue(ClaimTypes.Email),
                activeTeam, permissions.Order().ToList()));
        });

        g.MapGet("/teams", async (ClaimsPrincipal me, IDispatcher d, CancellationToken ct) =>
            TypedResults.Ok(await d.QueryAsync(new UserTeamsQuery(me.UserId()), ct)));

        g.MapPut("/active-team", async Task<Results<NoContent, ProblemHttpResult>> (ActiveTeamRequest r, ClaimsPrincipal me, IDispatcher d, CancellationToken ct) =>
        {
            var id = me.UserId();
            // Só pode ativar um time do qual participa (null = branding da plataforma)
            if (r.TeamId is { } teamId && !(await d.QueryAsync(new UserTeamsQuery(id), ct)).Any(t => t.Id == teamId))
                return TypedResults.Problem("Você não participa deste time.", statusCode: StatusCodes.Status403Forbidden);
            await d.SendAsync(new SetActiveTeamCommand(id, r.TeamId), ct);
            return TypedResults.NoContent();
        });

        // ---------------- Notificações
        g.MapGet("/notifications", async (int? take, ClaimsPrincipal me, IDispatcher d, CancellationToken ct) =>
            TypedResults.Ok(await d.QueryAsync(new MyNotificationsQuery(me.UserId(), Math.Clamp(take ?? 20, 1, ApiEndpoints.MaxPageSize)), ct)));

        g.MapPost("/notifications/read", async (ClaimsPrincipal me, IDispatcher d, CancellationToken ct) =>
        {
            await d.SendAsync(new MarkNotificationsReadCommand(me.UserId()), ct);
            return TypedResults.NoContent();
        });

        g.MapPost("/notifications/{id:int}/read", async (int id, ClaimsPrincipal me, IDispatcher d, CancellationToken ct) =>
        {
            await d.SendAsync(new MarkNotificationsReadCommand(me.UserId(), id), ct);
            return TypedResults.NoContent();
        });

        g.MapDelete("/notifications/read", async (ClaimsPrincipal me, IDispatcher d, CancellationToken ct) =>
        {
            await d.SendAsync(new ClearReadNotificationsCommand(me.UserId()), ct);
            return TypedResults.NoContent();
        });
    }
}
