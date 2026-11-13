using BlazorApp3.Features.Account;
using BlazorApp3.Services;

namespace BlazorApp3.Features.Users;

/// <summary>Linha da listagem de usuários (com papéis e times já resolvidos).</summary>
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
    public bool EmailConfirmed { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public DateTimeOffset? LockoutEnd { get; init; }
    public DateTime? BlockedAt { get; init; }
    public string? BlockReason { get; init; }
    public string? BlockedBy { get; init; }

    /// <summary>Bloqueado agora (pelo administrador ou por excesso de tentativas).</summary>
    public bool IsBlocked => BlockState.IsLocked(LockoutEnd);
    public bool IsAdminBlock => IsBlocked && BlockedAt is not null;
    public List<Badge> Roles { get; set; } = [];
    public List<Badge> Teams { get; set; } = [];
}
