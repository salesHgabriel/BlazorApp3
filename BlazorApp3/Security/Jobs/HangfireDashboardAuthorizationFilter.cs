using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authorization;

namespace BlazorApp3.Security;

/// <summary>Painel do Hangfire (/jobs) só para usuários autenticados com a permissão <see cref="Perm.JobsView"/>.</summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAsyncAuthorizationFilter
{
    public async Task<bool> AuthorizeAsync(DashboardContext context)
    {
        var http = context.GetHttpContext();
        if (http.User.Identity?.IsAuthenticated != true) return false;
        var auth = http.RequestServices.GetRequiredService<IAuthorizationService>();
        return (await auth.AuthorizeAsync(http.User, Perm.Policy(Perm.JobsView))).Succeeded;
    }
}
