using System.Security.Claims;
using System.Text.Json.Serialization;
using BlazorApp3.Api.Auth;
using BlazorApp3.Api.Platform;
using BlazorApp3.Api.Tenant;
using BlazorApp3.Security;
using BlazorApp3.Services;

namespace BlazorApp3.Api;

/// <summary>
/// API REST (/api/*) sobre o mesmo CQRS usado pelas telas: cada endpoint só traduz HTTP ⇄ query/command.
/// Autenticação por JWT (header Authorization: Bearer, ver /api/auth) ou pelo cookie do Identity;
/// autorização pelas mesmas permissões fixas (Perm.*).
/// </summary>
public static class ApiEndpoints
{
    public const string Prefix = "/api";
    public const int MaxPageSize = 100;

    public static IServiceCollection AddRestApi(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddApiDocumentation();
        services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        return services;
    }

    public static IEndpointRouteBuilder MapRestApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup(Prefix)
            .RequireAuthorization()
            .AddEndpointFilter<AppExceptionFilter>();

        api.MapAuthEndpoints();
        api.MapMeEndpoints();
        api.MapDashboardEndpoints();
        api.MapUsersEndpoints();
        api.MapRolesEndpoints();
        api.MapPermissionsEndpoints();
        api.MapTeamsEndpoints();
        api.MapBrandingEndpoints();
        api.MapSettingsEndpoints();
        return app;
    }

    /// <summary>Exige QUALQUER uma das permissões (mesma policy do [HasPermission]).</summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, params string[] permissions)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(Perm.AnyPolicy(permissions));

    public static bool IsApiRequest(this HttpRequest request) =>
        request.Path.StartsWithSegments(Prefix, StringComparison.OrdinalIgnoreCase);

    public static string UserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Usuário sem identificador.");

    public static string DisplayName(this ClaimsPrincipal user) =>
        user.FindFirstValue("full_name") ?? user.Identity?.Name ?? "";

    public static int ClampPageSize(int? pageSize, int fallback = 10) => Math.Clamp(pageSize ?? fallback, 1, MaxPageSize);
    public static int ClampPage(int? page) => Math.Max(page ?? 1, 1);
}

/// <summary>Regras de negócio (AppException) viram 400 ProblemDetails, como o toast de erro das telas.</summary>
internal sealed class AppExceptionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (AppException ex)
        {
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Requisição inválida");
        }
    }
}

/// <summary>Corpo comum das ações em lote.</summary>
public sealed record IdsRequest(IReadOnlyList<string> Ids);
