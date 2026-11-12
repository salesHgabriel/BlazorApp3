using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Users;

/// <summary>Ativa/inativa usuários em lote. Inativar derruba as sessões abertas.</summary>
public sealed record SetUsersActiveCommand(IReadOnlyList<string> Ids, bool Active) : ICommand, IInvalidatesPermissions;
