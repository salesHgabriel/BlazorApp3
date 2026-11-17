using System.Security.Claims;
using System.Text;
using BlazorApp3.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BlazorApp3.Api.Auth;

/// <summary>Seção "Jwt" do appsettings. Em produção, defina a chave fora do repositório (ex.: variável Jwt__SigningKey).</summary>
public sealed class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = "BlazorApp3";
    public string Audience { get; set; } = "BlazorApp3.Api";
    /// <summary>Chave HMAC-SHA256 com pelo menos 32 bytes.</summary>
    public string SigningKey { get; set; } = "";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;

    public SymmetricSecurityKey Key() => new(Encoding.UTF8.GetBytes(SigningKey));
}

public static class JwtAuthentication
{
    /// <summary>Cookie para o app Blazor; Bearer (JWT) para a API quando o header Authorization vier preenchido.</summary>
    public const string Scheme = "CookieOrBearer";

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Section))
            .Validate(o => Encoding.UTF8.GetByteCount(o.SigningKey) >= 32, "Jwt:SigningKey precisa ter pelo menos 32 bytes (defina-a em appsettings, user-secrets ou na variável Jwt__SigningKey).")
            .Validate(o => o.AccessTokenMinutes > 0 && o.RefreshTokenDays > 0, "Jwt:AccessTokenMinutes e Jwt:RefreshTokenDays devem ser positivos.")
            .ValidateOnStart();

        services.AddScoped<JwtTokenService>();

        // Registrado depois do AddIdentity: substitui os esquemas padrão pelo seletor abaixo
        services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = Scheme;
                o.DefaultChallengeScheme = Scheme;
                o.DefaultForbidScheme = Scheme;
            })
            .AddPolicyScheme(Scheme, Scheme, o => o.ForwardDefaultSelector = ctx =>
                ctx.Request.IsApiRequest() &&
                ctx.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? JwtBearerDefaults.AuthenticationScheme
                    : IdentityConstants.ApplicationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((o, jwt) =>
            {
                var cfg = jwt.Value;
                o.MapInboundClaims = false; // mantém os tipos de claim exatamente como no cookie (NameIdentifier, full_name...)
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = cfg.Issuer,
                    ValidAudience = cfg.Audience,
                    IssuerSigningKey = cfg.Key(),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                o.Events = new JwtBearerEvents
                {
                    // Mesmo controle de sessão do cookie: logout, sessão única, bloqueio e inativação invalidam o token na hora
                    OnTokenValidated = async ctx =>
                    {
                        var sessions = ctx.HttpContext.RequestServices.GetRequiredService<UserSessions>();
                        if (ctx.Principal is null || !await sessions.IsCurrentAsync(ctx.Principal))
                        {
                            ctx.Fail("Sessão encerrada.");
                            return;
                        }
                        await sessions.TouchAsync(ctx.Principal.FindFirstValue(UserSessions.ClaimType));
                    }
                };
            });

        return services;
    }
}
