using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Users;

/// <summary>Remove o bloqueio (administrativo ou por excesso de tentativas) e zera as falhas de login.</summary>
public sealed record UnblockUsersCommand(IReadOnlyList<string> Ids) : ICommand;
