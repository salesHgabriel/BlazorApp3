using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace BlazorApp3.Api;

/// <summary>
/// Documento OpenAPI (/openapi/v1.json) só com as rotas /api e interface Scalar (/scalar).
/// Publicados apenas em Development. Para testar: POST /api/auth/token, copie o accessToken e cole em "Authentication" (Bearer).
/// </summary>
public static class ApiDocumentation
{
    public const string ScalarPath = "/scalar";
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services) =>
        services.AddOpenApi(o =>
        {
            // Fora as páginas Blazor e endpoints internos (logout do navegador, troca de idioma...)
            o.ShouldInclude = d => d.RelativePath?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true;

            o.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "BlazorApp3 API",
                    Version = "v1",
                    Description = "API REST do BlazorApp3. Autentique em POST /api/auth/token e envie o accessToken no header Authorization: Bearer. " +
                                  "Quando expirar, use POST /api/auth/refresh."
                };
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes[BearerScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Access token devolvido por POST /api/auth/token."
                };
                return Task.CompletedTask;
            });

            // Exige o Bearer em toda rota protegida (as [AllowAnonymous] — login e refresh — ficam sem cadeado)
            o.AddOperationTransformer((operation, context, _) =>
            {
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;
                if (metadata.OfType<IAllowAnonymous>().Any() || !metadata.OfType<IAuthorizeData>().Any()) return Task.CompletedTask;
                operation.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(BearerScheme, context.Document)] = [] }];
                return Task.CompletedTask;
            });
        });

    public static WebApplication MapApiDocumentation(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;

        app.MapOpenApi();
        app.MapScalarApiReference(ScalarPath, o => o
            .WithTitle("BlazorApp3 API")
            .AddPreferredSecuritySchemes(BearerScheme));
        return app;
    }
}
