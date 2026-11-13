using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Features.Account;
using BlazorApp3.Localization;
using BlazorApp3.Services;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp3.Features.Users;

internal sealed class ResendConfirmationHandler(UserManager<AppUser> users, EmailConfirmationService confirmation)
    : ICommandHandler<ResendConfirmationCommand, Unit>
{
    public async Task<Unit> HandleAsync(ResendConfirmationCommand command, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(command.UserId) ?? throw new AppException(AppText.Get("Usuário não encontrado."));
        var result = await confirmation.IssueCodeAsync(user, ct);
        return result.Status switch
        {
            CodeIssueStatus.AlreadyConfirmed => throw new AppException(AppText.Get("O e-mail deste usuário já está confirmado.")),
            CodeIssueStatus.Cooldown => throw new AppException(AppText.Format("Um código foi enviado há pouco. Aguarde {0} segundos.", result.CooldownSeconds)),
            _ => Unit.Value
        };
    }
}
