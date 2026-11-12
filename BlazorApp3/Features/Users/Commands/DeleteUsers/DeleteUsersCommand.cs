using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Users;

/// <summary>Exclui um ou mais usuários (vínculos com papéis e times saem em cascata).</summary>
public sealed record DeleteUsersCommand(IReadOnlyList<string> Ids) : ICommand, IInvalidatesPermissions;
