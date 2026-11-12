using System.ComponentModel.DataAnnotations;
using BlazorApp3.Localization;

namespace BlazorApp3.Features.Users;

public sealed class UserEditModel
{
    public string? Id { get; set; }

    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.NameRequired)), StringLength(150, MinimumLength = 3, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.NameLength3To150))]
    public string FullName { get; set; } = "";

    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.EmailRequired)), EmailAddress(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.EmailInvalid))]
    public string Email { get; set; } = "";

    [StringLength(100)] public string? JobTitle { get; set; }
    [StringLength(100)] public string? Department { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Obrigatória na criação; opcional na edição (redefine a senha).</summary>
    [StringLength(100, MinimumLength = 6, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.PasswordMinLength))]
    public string? Password { get; set; }

    public List<string> RoleIds { get; set; } = [];
    public List<int> TeamIds { get; set; } = [];
}
