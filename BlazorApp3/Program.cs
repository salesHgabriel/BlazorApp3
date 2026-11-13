using BlazorApp3;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Components;
using BlazorApp3.Data;
using BlazorApp3.Features.Account;
using BlazorApp3.Features.Notifications;
using BlazorApp3.Features.Settings;
using BlazorApp3.Services.Email;
using Hangfire;
using Hangfire.Storage.SQLite;
using BlazorApp3.Localization;
using BlazorApp3.Security;
using Microsoft.AspNetCore.Localization;
using BlazorApp3.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// ---------------- Idiomas (resource files em Resources/, cultura escolhida via cookie)
builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(o =>
{
    o.SetDefaultCulture(AppCultures.Default)
        .AddSupportedCultures(AppCultures.Names)
        .AddSupportedUICultures(AppCultures.Names);
    o.ApplyCurrentCultureToResponseHeaders = true;
});

// ---------------- Banco (SQLite) + Identity
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db";
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(connectionString));

builder.Services.AddIdentity<AppUser, AppRole>(o =>
    {
        o.Password.RequiredLength = 6;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireUppercase = true;
        o.Password.RequireDigit = true;
        o.User.RequireUniqueEmail = true;
        // Confirmação de e-mail é verificada no login conforme a configuração do sistema (pode ser desligada na tela)
        o.SignIn.RequireConfirmedAccount = false;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddErrorDescriber<LocalizedIdentityErrorDescriber>()
    .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.LogoutPath = "/Account/Logout";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = true;
    o.Events.OnValidatePrincipal = UserSessions.ValidatePrincipalAsync; // security stamp + sessão (tabela UserSessions)
});
// O security stamp validator recria o principal: mantém o session_id e o amr (MFA) do login
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.OnRefreshingPrincipal = UserSessions.CarryOverClaimsAsync);

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// ---------------- Autorização baseada em permissões fixas
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<PermissionService>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddAuthorization();

// ---------------- CQRS (dispatcher próprio, só DI): handlers registrados por varredura do assembly
builder.Services.AddCqrs(typeof(Program).Assembly)
    .AddPipelineBehavior<LoggingBehavior>()          // 1º: mais externo
    .AddPipelineBehavior<ValidationBehavior>()       // 2º: DataAnnotations no servidor
    .AddPipelineBehavior<PermissionCacheBehavior>(); // 3º: invalida cache após commands de segurança

// ---------------- Configurações do sistema, sessões, cadastro/confirmação, e-mail e notificações
builder.Services.AddSingleton<SystemSettingsProvider>();
builder.Services.AddSingleton<UserSessions>();
builder.Services.AddSingleton<AppUrls>();
builder.Services.Configure<AccountOptions>(builder.Configuration.GetSection(AccountOptions.Section));
builder.Services.AddScoped<EmailConfirmationService>();
builder.Services.AddScoped<ReauthVerifier>();
builder.Services.AddScoped<SessionEvents>();
builder.Services.AddEmail(builder.Configuration); // provedor escolhido em Email:Provider (Pickup, Smtp, SendGrid, Mailtrap...)
builder.Services.AddScoped<INotifier, Notifier>();
builder.Services.AddSignalR();

// ---------------- Hangfire (jobs em segundo plano: e-mails, bloqueios agendados, limpezas recorrentes)
// Banco próprio (hangfire.db) para não disputar lock de escrita do SQLite com o app.db
builder.Services.AddHangfire(c => c
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSQLiteStorage(builder.Configuration.GetConnectionString("Hangfire") ?? "hangfire.db", new SQLiteStorageOptions()));
builder.Services.AddHangfireServer(o =>
{
    o.Queues = JobQueues.All;
    o.WorkerCount = Math.Min(Environment.ProcessorCount, 4);
});

// ---------------- Serviços de UI
builder.Services.AddSingleton<FileStorageService>();
builder.Services.AddScoped<BrandingState>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<LoadingService>();
builder.Services.AddScoped<ConfirmService>();
builder.Services.AddScoped<UiDispatcher>();
builder.Services.AddScoped<BlazorApp3.Services.Reports.ReportService>();

// QuestPDF (relatórios PDF): licença Community, gratuita para empresas com receita anual bruta abaixo de US$ 1 mi.
// Acima disso é preciso uma licença comercial: https://www.questpdf.com/license/
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var app = builder.Build();

// ---------------- Criação do banco + seeds
await using (var scope = app.Services.CreateAsyncScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
    // Conexões SignalR registradas antes de reiniciar não existem mais
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().UserConnections.ExecuteDeleteAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Guarda a URL pública na primeira requisição (links dos e-mails enviados pelos jobs)
var appUrls = app.Services.GetRequiredService<AppUrls>();
app.Use((ctx, next) => { appUrls.Capture(ctx.Request); return next(ctx); });

// Arquivos enviados pelos usuários (logos, avatares, anexos)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(app.Services.GetRequiredService<FileStorageService>().RootPath),
    RequestPath = FileStorageService.RequestPath
});

// Define CurrentCulture/CurrentUICulture da requisição (cookie → Accept-Language → padrão).
// O circuito Blazor herda a cultura da conexão, então tudo que roda nele usa o idioma escolhido.
app.UseRequestLocalization();

app.UseAuthentication();
app.UseAuthorization();

// Painel do Hangfire: exige login + permissão Jobs.View
app.UseHangfireDashboard("/jobs", new DashboardOptions
{
    Authorization = [],
    AsyncAuthorization = [new HangfireDashboardAuthorizationFilter()],
    DashboardTitle = "Jobs",
    AppPath = "/",
    DarkModeEnabled = false
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapHub<NotificationsHub>(NotificationsHub.Path);
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorApp3.Client._Imports).Assembly);

// Logout: a página /Account/Logout (SSR estático) envia o formulário com antiforgery para este endpoint
app.MapPost("/Account/PerformLogout", async (SignInManager<AppUser> signInManager, UserSessions sessions, HttpContext http) =>
{
    var sid = http.User.FindFirstValue(UserSessions.ClaimType);
    if (sid is not null) await sessions.RevokeAsync(sid, SessionRevokeReason.Logout);
    await signInManager.SignOutAsync();
    return TypedResults.LocalRedirect("~/Account/Login");
});

// Troca de idioma: grava o cookie de cultura e volta para a página (recarregada, o circuito novo nasce no idioma novo)
app.MapGet("/culture/set", (string culture, string? redirectUri, HttpContext http) =>
{
    if (AppCultures.IsSupported(culture))
    {
        http.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax, HttpOnly = true });
    }
    // Só aceita caminhos locais ("/x", não "//host" nem "/\host"): evita open redirect
    var isLocal = !string.IsNullOrEmpty(redirectUri) && redirectUri[0] == '/' &&
                  (redirectUri.Length == 1 || (redirectUri[1] != '/' && redirectUri[1] != '\\'));
    return TypedResults.LocalRedirect(isLocal ? redirectUri! : "~/");
});

// ---------------- Jobs recorrentes (idempotente: atualiza se já existir)
var recurring = app.Services.GetRequiredService<IRecurringJobManager>();
recurring.AddOrUpdate<AccountMaintenanceJobs>(AccountMaintenanceJobs.PurgeUnconfirmedId, j => j.PurgeUnconfirmedAccountsAsync(), Cron.Daily(3));
recurring.AddOrUpdate<AccountMaintenanceJobs>(AccountMaintenanceJobs.PurgeCodesId, j => j.PurgeExpiredCodesAsync(), Cron.Hourly());

app.Run();
