using Microsoft.AspNetCore.Identity;

namespace BlazorApp3.Services;

/// <summary>Erro de regra de negócio, exibido ao usuário.</summary>
public sealed class AppException(string message) : Exception(message)
{
    public static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new AppException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}
