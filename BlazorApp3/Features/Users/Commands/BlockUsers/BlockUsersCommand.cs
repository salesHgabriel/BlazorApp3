using System.ComponentModel.DataAnnotations;
using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Users;

/// <summary>
/// Bloqueia usuários (lockout do Identity) com motivo opcional. <paramref name="Hours"/> nulo = tempo indeterminado.
/// Derruba as sessões abertas, avisa o usuário por e-mail e, se temporário, agenda o fim do bloqueio (Hangfire).
/// </summary>
public sealed record BlockUsersCommand(
    IReadOnlyList<string> Ids,
    [property: StringLength(300)] string? Reason,
    [property: Range(1, 24 * 365)] int? Hours,
    string BlockedBy) : ICommand;
