using Microsoft.AspNetCore.Authorization;

namespace BlazorApp3.Security;

/// <summary>[HasPermission(Perm.UsersView)] em páginas/componentes (qualquer uma das permissões basta).</summary>
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(params string[] permissions) => Policy = Perm.AnyPolicy(permissions);
}
