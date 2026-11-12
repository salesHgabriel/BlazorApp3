using System.ComponentModel.DataAnnotations;
using BlazorApp3.Localization;

namespace BlazorApp3.Features.Teams;

public sealed class TeamEditModel
{
    public int? Id { get; set; }

    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.TeamNameRequired)), StringLength(100, MinimumLength = 2, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.NameLength2To100))]
    public string Name { get; set; } = "";

    [StringLength(100), RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.SlugFormat))]
    public string? Slug { get; set; }

    [StringLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public bool UseCustomBranding { get; set; }
    [StringLength(100)] public string? DisplayName { get; set; }
    public string? LogoUrl { get; set; }

    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.ColorRequired)), RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.ColorInvalid))]
    public string PrimaryColor { get; set; } = "#4f46e5";
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.ColorInvalid))]
    public string? AccentColor { get; set; }
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.ColorInvalid))]
    public string? SidebarColor { get; set; }
}
