using System.Security.Claims;
using BlazorApp3.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace BlazorApp3.Security;

/// <summary>Adiciona o nome completo ao cookie. O session_id entra no login (UserSessions.SignInAsync).</summary>
public sealed class AppClaimsPrincipalFactory(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<AppUser, AppRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("full_name", user.FullName));
        return identity;
    }
}
