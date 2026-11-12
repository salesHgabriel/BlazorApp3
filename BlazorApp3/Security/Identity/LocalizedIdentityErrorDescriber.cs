using BlazorApp3.Localization;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp3.Security;

/// <summary>Mensagens de erro do Identity no idioma atual (resource files).</summary>
public sealed class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DuplicateEmail(string email) => new() { Code = nameof(DuplicateEmail), Description = AppText.Format("O e-mail '{0}' já está em uso.", email) };
    public override IdentityError DuplicateUserName(string userName) => new() { Code = nameof(DuplicateUserName), Description = AppText.Format("O login '{0}' já está em uso.", userName) };
    public override IdentityError InvalidEmail(string? email) => new() { Code = nameof(InvalidEmail), Description = AppText.Format("O e-mail '{0}' é inválido.", email) };
    public override IdentityError DuplicateRoleName(string role) => new() { Code = nameof(DuplicateRoleName), Description = AppText.Format("Já existe um papel chamado '{0}'.", role) };
    public override IdentityError InvalidRoleName(string? role) => new() { Code = nameof(InvalidRoleName), Description = AppText.Format("Nome de papel inválido: '{0}'.", role) };
    public override IdentityError PasswordTooShort(int length) => new() { Code = nameof(PasswordTooShort), Description = AppText.Format("A senha deve ter no mínimo {0} caracteres.", length) };
    public override IdentityError PasswordRequiresDigit() => new() { Code = nameof(PasswordRequiresDigit), Description = AppText.Get("A senha deve conter ao menos um número.") };
    public override IdentityError PasswordRequiresLower() => new() { Code = nameof(PasswordRequiresLower), Description = AppText.Get("A senha deve conter ao menos uma letra minúscula.") };
    public override IdentityError PasswordRequiresUpper() => new() { Code = nameof(PasswordRequiresUpper), Description = AppText.Get("A senha deve conter ao menos uma letra maiúscula.") };
    public override IdentityError PasswordRequiresNonAlphanumeric() => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = AppText.Get("A senha deve conter ao menos um caractere especial.") };
    public override IdentityError PasswordMismatch() => new() { Code = nameof(PasswordMismatch), Description = AppText.Get("Senha incorreta.") };
}
