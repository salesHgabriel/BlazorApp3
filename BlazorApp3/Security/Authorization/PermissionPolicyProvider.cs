using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace BlazorApp3.Security;

/// <summary>Cria policies dinamicamente para nomes no formato "perm:A|B".</summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Perm.PolicyPrefix, StringComparison.Ordinal))
        {
            var perms = policyName[Perm.PolicyPrefix.Length..].Split('|', StringSplitOptions.RemoveEmptyEntries);
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(perms))
                .Build();
        }
        return await base.GetPolicyAsync(policyName);
    }
}
