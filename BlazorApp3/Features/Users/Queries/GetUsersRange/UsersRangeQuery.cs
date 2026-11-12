using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Users;

/// <summary>Faixa arbitrária (skip/take) para listas virtualizadas.</summary>
public sealed record UsersRangeQuery(int Skip, int Take) : IQuery<UsersRange>;
