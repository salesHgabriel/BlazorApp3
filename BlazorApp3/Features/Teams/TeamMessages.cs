using System.ComponentModel.DataAnnotations;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Features.Users;
using BlazorApp3.Services;

namespace BlazorApp3.Features.Teams;

// ------------------------------------------------------------------ Queries
public sealed class SearchTeamsQuery : PagedQuery<TeamListItem>
{
    public SearchTeamsQuery() => PageSize = 9;

    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public bool? CustomBranding { get; set; }
    public string SortBy { get; set; } = "name";
}

public sealed class TeamMembersQuery(int teamId) : PagedQuery<TeamMemberItem>
{
    public int TeamId { get; } = teamId;
    public string? Search { get; set; }
    public TeamRole? Role { get; set; }
}

public sealed record TeamSummaryQuery(int Id) : IQuery<TeamListItem?>;
public sealed record TeamOptionsQuery : IQuery<List<SelectOption<int>>>;
public sealed record GetTeamForEditQuery(int Id) : IQuery<TeamEditModel?>;
public sealed record TeamRoleCountsQuery(int TeamId) : IQuery<Dictionary<TeamRole, int>>;
public sealed record UserTeamsQuery(string UserId) : IQuery<List<TeamOption>>;
public sealed record ActiveTeamIdQuery(string UserId) : IQuery<int?>;

// ------------------------------------------------------------------ Commands
public sealed record SaveTeamCommand(TeamEditModel Model) : ICommand<int>;
public sealed record DeleteTeamCommand(int Id) : ICommand;
public sealed record AddTeamMemberCommand(int TeamId, string UserId, TeamRole Role) : ICommand;
public sealed record UpdateTeamMemberRoleCommand(int TeamId, string UserId, TeamRole Role) : ICommand;
public sealed record RemoveTeamMemberCommand(int TeamId, string UserId) : ICommand;
public sealed record SetActiveTeamCommand(string UserId, int? TeamId) : ICommand;

// ------------------------------------------------------------------ DTOs
public static class TeamRoles
{
    public static string Label(TeamRole role) => role switch
    {
        TeamRole.Owner => "Proprietário",
        TeamRole.Admin => "Administrador",
        _ => "Membro"
    };
}

public sealed class TeamListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";
    public string? Description { get; init; }
    public string? DisplayName { get; init; }
    public string? LogoUrl { get; init; }
    public string PrimaryColor { get; init; } = "";
    public string? AccentColor { get; init; }
    public string? SidebarColor { get; init; }
    public bool IsActive { get; init; }
    public bool UseCustomBranding { get; init; }
    public int MemberCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<UserLookup> TopMembers { get; set; } = [];
}

public sealed class TeamEditModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Informe o nome do time"), StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres")]
    public string Name { get; set; } = "";

    [StringLength(100), RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessage = "Use apenas letras minúsculas, números e hífens")]
    public string? Slug { get; set; }

    [StringLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public bool UseCustomBranding { get; set; }
    [StringLength(100)] public string? DisplayName { get; set; }
    public string? LogoUrl { get; set; }

    [Required, RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Cor inválida")]
    public string PrimaryColor { get; set; } = "#4f46e5";
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Cor inválida")]
    public string? AccentColor { get; set; }
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Cor inválida")]
    public string? SidebarColor { get; set; }
}

public sealed class TeamMemberItem
{
    public string UserId { get; init; } = "";
    public string FullName { get; init; } = "";
    public string Email { get; init; } = "";
    public string? AvatarUrl { get; init; }
    public string? JobTitle { get; init; }
    public bool IsActive { get; init; }
    public TeamRole Role { get; init; }
    public DateTime JoinedAt { get; init; }
}

/// <summary>Time disponível para o usuário (seletor de time + branding).</summary>
public sealed record TeamOption(int Id, string Name, string? DisplayName, string? LogoUrl, string PrimaryColor,
    string? AccentColor, string? SidebarColor, bool UseCustomBranding, TeamRole Role);
