using BlazorApp3.Application.Cqrs;
using BlazorApp3.Features.Roles;
using Microsoft.Extensions.Caching.Memory;

namespace BlazorApp3.Security;

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
