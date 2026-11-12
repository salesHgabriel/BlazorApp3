using System.ComponentModel.DataAnnotations;
using BlazorApp3.Localization;

namespace BlazorApp3.Features.Roles;

public sealed class RoleEditModel
{
    public string? Id { get; set; }
    public bool IsSystem { get; set; }

    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.RoleNameRequired)), StringLength(60, MinimumLength = 3, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.NameLength3To60))]
    public string Name { get; set; } = "";

    [StringLength(300)] public string? Description { get; set; }

    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.ColorInvalid))]
    public string Color { get; set; } = "#6366f1";

    public HashSet<int> PermissionIds { get; set; } = [];
}
