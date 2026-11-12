using System.Security.Claims;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Features.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BlazorApp3.Security;

/// <summary>[HasPermission(Perm.UsersView)] em páginas/componentes.</summary>
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(params string[] permissions) => Policy = Perm.AnyPolicy(permissions);
}

public sealed class PermissionRequirement(IReadOnlyList<string> permissions) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; } = permissions;
}

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

public sealed class PermissionHandler(PermissionService permissions) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return;

        var granted = await permissions.GetPermissionsAsync(userId);
        if (requirement.Permissions.Any(granted.Contains))
            context.Succeed(requirement);
    }
}

/// <summary>
/// Resolve as permissões efetivas do usuário via <see cref="UserPermissionsQuery"/>, com cache.
/// Commands marcados com IInvalidatesPermissions invalidam o cache automaticamente (PermissionCacheBehavior).
/// </summary>
public sealed class PermissionService(IDispatcher dispatcher, IMemoryCache cache)
{
    private int _version;

    public async Task<IReadOnlySet<string>> GetPermissionsAsync(string userId)
    {
        var key = $"perms:{Volatile.Read(ref _version)}:{userId}";
        var result = await cache.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return dispatcher.QueryAsync(new UserPermissionsQuery(userId));
        });
        return result ?? [];
    }

    /// <summary>Invalida o cache de todos os usuários.</summary>
    public void Invalidate() => Interlocked.Increment(ref _version);
}
