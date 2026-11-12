using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace BlazorApp3.Security;

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
