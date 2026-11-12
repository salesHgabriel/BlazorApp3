namespace BlazorApp3.Features.Users;

/// <summary>Fatia (skip/take) de usuários + total, para listas virtualizadas.</summary>
public sealed record UsersRange(List<UserLookup> Items, int Total);
