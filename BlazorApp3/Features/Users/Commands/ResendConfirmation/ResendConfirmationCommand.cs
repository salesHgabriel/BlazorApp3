using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Users;

/// <summary>Administrador reenvia o código de confirmação de e-mail para um usuário pendente.</summary>
public sealed record ResendConfirmationCommand(string UserId) : ICommand;
