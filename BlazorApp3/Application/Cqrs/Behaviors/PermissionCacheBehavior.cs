using BlazorApp3.Security;

namespace BlazorApp3.Application.Cqrs;

/// <summary>Invalida o cache de permissões após commands marcados com <see cref="IInvalidatesPermissions"/>.</summary>
public sealed class PermissionCacheBehavior(PermissionService permissions) : IPipelineBehavior
{
    public async Task<object?> HandleAsync(IMessage message, Func<Task<object?>> next, CancellationToken ct)
    {
        var result = await next();
        if (message is IInvalidatesPermissions) permissions.Invalidate();
        return result;
    }
}
