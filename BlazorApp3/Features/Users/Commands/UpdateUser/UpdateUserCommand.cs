using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Users;

/// <summary>Atualiza dados, senha (opcional), papéis e times do usuário.</summary>
public sealed record UpdateUserCommand(UserEditModel Model) : ICommand, IInvalidatesPermissions;
