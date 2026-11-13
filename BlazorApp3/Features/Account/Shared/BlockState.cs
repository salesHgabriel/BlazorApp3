using BlazorApp3.Data;

namespace BlazorApp3.Features.Account;

/// <summary>
/// Regras do bloqueio de conta. O bloqueio usa o lockout do Identity (LockoutEnd), o mesmo do excesso de
/// tentativas de senha; BlockedAt/BlockReason distinguem o bloqueio feito por um administrador.
/// </summary>
public static class BlockState
{
    /// <summary>LockoutEnd usado para bloqueio por tempo indeterminado.</summary>
    public static readonly DateTimeOffset Permanent = DateTimeOffset.MaxValue;

    public static bool IsLocked(DateTimeOffset? lockoutEnd) => lockoutEnd is { } end && end > DateTimeOffset.UtcNow;
    public static bool IsLocked(AppUser u) => IsLocked(u.LockoutEnd);
    public static bool IsAdminBlock(AppUser u) => u.BlockedAt is not null && IsLocked(u);
    public static bool IsPermanent(DateTimeOffset? end) => end is { } e && e > DateTimeOffset.MaxValue.AddYears(-1);

    public static void Clear(AppUser user)
    {
        user.BlockedAt = null;
        user.BlockReason = null;
        user.BlockedBy = null;
    }
}
