using Microsoft.AspNetCore.Authorization;

namespace BlazorApp3.Security;

public sealed class PermissionRequirement(IReadOnlyList<string> permissions) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; } = permissions;
}
