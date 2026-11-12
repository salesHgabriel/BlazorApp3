using System.ComponentModel.DataAnnotations;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Services;

namespace BlazorApp3.Features.Users;

// ------------------------------------------------------------------ Queries
public sealed class SearchUsersQuery : PagedQuery<UserListItem>
{
    public string? Search { get; set; }
    public List<string> RoleIds { get; set; } = [];
    public List<int> TeamIds { get; set; } = [];
    public List<string> Departments { get; set; } = [];
    public bool? IsActive { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDesc { get; set; }

    public int ActiveFilterCount =>
        (RoleIds.Count > 0 ? 1 : 0) + (TeamIds.Count > 0 ? 1 : 0) + (Departments.Count > 0 ? 1 : 0) +
        (IsActive.HasValue ? 1 : 0) + (CreatedFrom.HasValue ? 1 : 0) + (CreatedTo.HasValue ? 1 : 0);
}

public sealed record LookupUsersQuery(string? Term, int Page, int PageSize, int? ExcludeTeamId = null) : IQuery<PagedResult<UserLookup>>;

/// <summary>Faixa arbitrária (skip/take) para listas virtualizadas.</summary>
public sealed record UsersRangeQuery(int Skip, int Take) : IQuery<UsersRange>;

public sealed record GetDepartmentsQuery : IQuery<List<string>>;

public sealed record GetUserForEditQuery(string Id) : IQuery<UserEditModel?>;

// ------------------------------------------------------------------ Commands
public sealed record CreateUserCommand(UserEditModel Model) : ICommand<string>, IInvalidatesPermissions;
public sealed record UpdateUserCommand(UserEditModel Model) : ICommand, IInvalidatesPermissions;
public sealed record SetUsersActiveCommand(IReadOnlyList<string> Ids, bool Active) : ICommand, IInvalidatesPermissions;
public sealed record DeleteUsersCommand(IReadOnlyList<string> Ids) : ICommand, IInvalidatesPermissions;

// ------------------------------------------------------------------ DTOs

public sealed class UserListItem
{
    public string Id { get; init; } = "";
    public string FullName { get; init; } = "";
    public string Email { get; init; } = "";
    public string? JobTitle { get; init; }
    public string? Department { get; init; }
    public string? AvatarUrl { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public List<Badge> Roles { get; set; } = [];
    public List<Badge> Teams { get; set; } = [];
}

public sealed record UserLookup(string Id, string FullName, string Email, string? AvatarUrl, string? Department);

public sealed record UsersRange(List<UserLookup> Items, int Total);

public sealed class UserEditModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Informe o nome"), StringLength(150, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 150 caracteres")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Informe o e-mail"), EmailAddress(ErrorMessage = "E-mail inválido")]
    public string Email { get; set; } = "";

    [StringLength(100)] public string? JobTitle { get; set; }
    [StringLength(100)] public string? Department { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Obrigatória na criação; opcional na edição (redefine a senha).</summary>
    [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres")]
    public string? Password { get; set; }

    public List<string> RoleIds { get; set; } = [];
    public List<int> TeamIds { get; set; } = [];
}
