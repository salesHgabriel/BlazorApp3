using BlazorApp3.Application.Cqrs;
using BlazorApp3.Components;
using BlazorApp3.Data;
using BlazorApp3.Security;
using BlazorApp3.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

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
        o.SignIn.RequireConfirmedAccount = false;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddErrorDescriber<PortugueseIdentityErrorDescriber>()
    .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.LogoutPath = "/Account/Logout";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = true;
});

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

// ---------------- Serviços de UI
builder.Services.AddSingleton<FileStorageService>();
builder.Services.AddScoped<BrandingState>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<LoadingService>();
builder.Services.AddScoped<ConfirmService>();
builder.Services.AddScoped<UiDispatcher>();

var app = builder.Build();

// ---------------- Criação do banco + seeds
await using (var scope = app.Services.CreateAsyncScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Arquivos enviados pelos usuários (logos, avatares, anexos)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(app.Services.GetRequiredService<FileStorageService>().RootPath),
    RequestPath = FileStorageService.RequestPath
});

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorApp3.Client._Imports).Assembly);

// Logout: a página /Account/Logout (SSR estático) envia o formulário com antiforgery para este endpoint
app.MapPost("/Account/PerformLogout", async (SignInManager<AppUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return TypedResults.LocalRedirect("~/Account/Login");
});


app.Run();
