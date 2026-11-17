using System.Security.Claims;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Features.Branding;
using BlazorApp3.Features.Dashboard;
using BlazorApp3.Features.Settings;
using BlazorApp3.Security;

namespace BlazorApp3.Api.Platform;

/// <summary>/api/dashboard, /api/branding e /api/settings — recursos globais da plataforma.</summary>
public static class SystemEndpoints
{
    public static void MapDashboardEndpoints(this RouteGroupBuilder api) =>
        api.MapGet("/dashboard", async (IDispatcher d, CancellationToken ct) =>
                TypedResults.Ok(await d.QueryAsync(new GetDashboardQuery(), ct)))
            .WithTags("Dashboard")
            .RequirePermission(Perm.DashboardView);

    public static void MapBrandingEndpoints(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/branding").WithTags("Branding");

        g.MapGet("/", async (IDispatcher d, CancellationToken ct) =>
            TypedResults.Ok(await d.QueryAsync(new GetBrandingQuery(), ct)))
            .RequirePermission(Perm.BrandingView);

        g.MapPut("/", async (BrandingSettings settings, IDispatcher d, CancellationToken ct) =>
        {
            await d.SendAsync(new SaveBrandingCommand(settings), ct);
            return TypedResults.NoContent();
        })
            .RequirePermission(Perm.BrandingEdit);

        g.MapPost("/reset", async (IDispatcher d, CancellationToken ct) =>
            TypedResults.Ok(await d.SendAsync(new ResetBrandingCommand(), ct)))
            .RequirePermission(Perm.BrandingEdit);
    }

    public static void MapSettingsEndpoints(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/settings").WithTags("Settings").RequirePermission(Perm.SettingsSystem);

        g.MapGet("/", async (IDispatcher d, CancellationToken ct) =>
            TypedResults.Ok(await d.QueryAsync(new GetSystemSettingsQuery(), ct)));

        g.MapPut("/", async (SystemSettings settings, IDispatcher d, CancellationToken ct) =>
        {
            await d.SendAsync(new SaveSystemSettingsCommand(settings), ct);
            return TypedResults.NoContent();
        });

        g.MapPost("/test-email", async (ClaimsPrincipal me, IDispatcher d, CancellationToken ct) =>
        {
            await d.SendAsync(new SendTestEmailCommand(me.UserId()), ct);
            return TypedResults.Accepted((string?)null);
        });
    }
}
