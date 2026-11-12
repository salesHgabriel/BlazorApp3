namespace BlazorApp3.Features.Users;

/// <summary>Resumo de usuário para buscas, autocomplete e listas compactas.</summary>
public sealed record UserLookup(string Id, string FullName, string Email, string? AvatarUrl, string? Department);
