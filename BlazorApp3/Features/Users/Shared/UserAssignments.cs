using BlazorApp3.Data;
using BlazorApp3.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Users;

/// <summary>Sincroniza papéis e times de um usuário (usado na criação e na edição).</summary>
internal static class UserAssignments
{
    public static async Task SyncRolesAsync(UserManager<AppUser> users, AppDbContext db, AppUser user, List<string> roleIds, CancellationToken ct)
    {
        var wanted = await db.Roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Name!).ToListAsync(ct);
        var current = await users.GetRolesAsync(user);
        var remove = current.Except(wanted).ToList();
        var add = wanted.Except(current).ToList();
        if (remove.Count > 0) AppException.ThrowIfFailed(await users.RemoveFromRolesAsync(user, remove));
        if (add.Count > 0) AppException.ThrowIfFailed(await users.AddToRolesAsync(user, add));
    }

    public static async Task SyncTeamsAsync(AppDbContext db, string userId, List<int> teamIds, CancellationToken ct)
    {
        var current = await db.TeamMembers.Where(tm => tm.UserId == userId).ToListAsync(ct);
        db.TeamMembers.RemoveRange(current.Where(c => !teamIds.Contains(c.TeamId)));
        foreach (var id in teamIds.Except(current.Select(c => c.TeamId)))
            db.TeamMembers.Add(new TeamMember { TeamId = id, UserId = userId, Role = TeamRole.Member });
        await db.SaveChangesAsync(ct);
    }
}
