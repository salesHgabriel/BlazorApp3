using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Users;

/// <summary>Cria o usuário e retorna o Id.</summary>
public sealed record CreateUserCommand(UserEditModel Model) : ICommand<string>, IInvalidatesPermissions;
