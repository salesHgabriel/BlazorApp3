using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Users;

internal sealed class UserQueryHandlers(AppDbContext db) :
    IQueryHandler<SearchUsersQuery, PagedResult<UserListItem>>,
    IQueryHandler<LookupUsersQuery, PagedResult<UserLookup>>,
    IQueryHandler<UsersRangeQuery, UsersRange>,
    IQueryHandler<GetDepartmentsQuery, List<string>>,
    IQueryHandler<GetUserForEditQuery, UserEditModel?>
{
    public async Task<PagedResult<UserListItem>> HandleAsync(SearchUsersQuery query, CancellationToken ct)
    {
        var q = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.LikePattern();
            q = q.Where(u => EF.Functions.Like(u.FullName, s, "\\") || EF.Functions.Like(u.Email!, s, "\\") || EF.Functions.Like(u.JobTitle!, s, "\\"));
        }
        if (query.RoleIds.Count > 0)
        {
            var roleIds = query.RoleIds;
            q = q.Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id && roleIds.Contains(ur.RoleId)));
        }
        if (query.TeamIds.Count > 0)
        {
            var teamIds = query.TeamIds;
            q = q.Where(u => db.TeamMembers.Any(tm => tm.UserId == u.Id && teamIds.Contains(tm.TeamId)));
        }
        if (query.Departments.Count > 0)
        {
            var deps = query.Departments;
            q = q.Where(u => u.Department != null && deps.Contains(u.Department));
        }
        if (query.IsActive.HasValue) q = q.Where(u => u.IsActive == query.IsActive.Value);
        if (query.CreatedFrom.HasValue) { var from = query.CreatedFrom.Value.Date; q = q.Where(u => u.CreatedAt >= from); }
        if (query.CreatedTo.HasValue) { var to = query.CreatedTo.Value.Date.AddDays(1); q = q.Where(u => u.CreatedAt < to); }

        q = (query.SortBy, query.SortDesc) switch
        {
            ("email", false) => q.OrderBy(u => u.Email),
            ("email", true) => q.OrderByDescending(u => u.Email),
            ("department", false) => q.OrderBy(u => u.Department).ThenBy(u => u.FullName),
            ("department", true) => q.OrderByDescending(u => u.Department).ThenBy(u => u.FullName),
            ("created", false) => q.OrderBy(u => u.CreatedAt),
            ("created", true) => q.OrderByDescending(u => u.CreatedAt),
            ("lastLogin", false) => q.OrderBy(u => u.LastLoginAt),
            ("lastLogin", true) => q.OrderByDescending(u => u.LastLoginAt),
            (_, true) => q.OrderByDescending(u => u.FullName),
            _ => q.OrderBy(u => u.FullName)
        };

        var page = await q.Select(u => new UserListItem
        {
            Id = u.Id, FullName = u.FullName, Email = u.Email!, JobTitle = u.JobTitle, Department = u.Department,
            AvatarUrl = u.AvatarUrl, IsActive = u.IsActive, CreatedAt = u.CreatedAt, LastLoginAt = u.LastLoginAt
        }).ToPagedAsync(query.Page, query.PageSize, ct);

        var ids = page.Items.Select(i => i.Id).ToList();
        var roles = await (from ur in db.UserRoles
                           join r in db.Roles on ur.RoleId equals r.Id
                           where ids.Contains(ur.UserId)
                           select new { ur.UserId, r.Name, r.Color }).ToListAsync(ct);
        var teams = await (from tm in db.TeamMembers
                           join t in db.Teams on tm.TeamId equals t.Id
                           where ids.Contains(tm.UserId)
                           select new { tm.UserId, t.Name, t.PrimaryColor }).ToListAsync(ct);

        foreach (var item in page.Items)
        {
            item.Roles = roles.Where(r => r.UserId == item.Id).Select(r => new Badge(r.Name!, r.Color)).OrderBy(b => b.Label).ToList();
            item.Teams = teams.Where(t => t.UserId == item.Id).Select(t => new Badge(t.Name, t.PrimaryColor)).OrderBy(b => b.Label).ToList();
        }
        return page;
    }

    public async Task<PagedResult<UserLookup>> HandleAsync(LookupUsersQuery query, CancellationToken ct)
    {
        var q = db.Users.AsNoTracking().Where(u => u.IsActive);
        if (!string.IsNullOrWhiteSpace(query.Term))
        {
            var s = query.Term.LikePattern();
            q = q.Where(u => EF.Functions.Like(u.FullName, s, "\\") || EF.Functions.Like(u.Email!, s, "\\"));
        }
        if (query.ExcludeTeamId is { } teamId)
            q = q.Where(u => !db.TeamMembers.Any(tm => tm.TeamId == teamId && tm.UserId == u.Id));

        return await q.OrderBy(u => u.FullName)
            .Select(u => new UserLookup(u.Id, u.FullName, u.Email!, u.AvatarUrl, u.Department))
            .ToPagedAsync(query.Page, query.PageSize, ct);
    }

    public async Task<UsersRange> HandleAsync(UsersRangeQuery query, CancellationToken ct)
    {
        var q = db.Users.AsNoTracking().OrderBy(u => u.FullName);
        var items = await q.Skip(query.Skip).Take(query.Take)
            .Select(u => new UserLookup(u.Id, u.FullName, u.Email!, u.AvatarUrl, u.Department)).ToListAsync(ct);
        return new UsersRange(items, await q.CountAsync(ct));
    }

    public Task<List<string>> HandleAsync(GetDepartmentsQuery query, CancellationToken ct) =>
        db.Users.Where(u => u.Department != null && u.Department != "")
            .Select(u => u.Department!).Distinct().OrderBy(d => d).ToListAsync(ct);

    public async Task<UserEditModel?> HandleAsync(GetUserForEditQuery query, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.Id, ct);
        if (user is null) return null;
        return new UserEditModel
        {
            Id = user.Id, FullName = user.FullName, Email = user.Email!, JobTitle = user.JobTitle,
            Department = user.Department, AvatarUrl = user.AvatarUrl, IsActive = user.IsActive,
            RoleIds = await db.UserRoles.Where(ur => ur.UserId == user.Id).Select(ur => ur.RoleId).ToListAsync(ct),
            TeamIds = await db.TeamMembers.Where(tm => tm.UserId == user.Id).Select(tm => tm.TeamId).ToListAsync(ct)
        };
    }
}

internal sealed class UserCommandHandlers(UserManager<AppUser> users, AppDbContext db) :
    ICommandHandler<CreateUserCommand, string>,
    ICommandHandler<UpdateUserCommand, Unit>,
    ICommandHandler<SetUsersActiveCommand, Unit>,
    ICommandHandler<DeleteUsersCommand, Unit>
{
    public async Task<string> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        var m = command.Model;
        if (string.IsNullOrWhiteSpace(m.Password))
            throw new AppException("Informe uma senha para o novo usuário.");

        var user = new AppUser
        {
            UserName = m.Email.Trim(), Email = m.Email.Trim(), EmailConfirmed = true,
            FullName = m.FullName.Trim(), JobTitle = m.JobTitle, Department = m.Department,
            AvatarUrl = m.AvatarUrl, IsActive = m.IsActive, CreatedAt = DateTime.UtcNow
        };
        AppException.ThrowIfFailed(await users.CreateAsync(user, m.Password));
        await SyncRolesAsync(user, m.RoleIds, ct);
        await SyncTeamsAsync(user.Id, m.TeamIds, ct);
        return user.Id;
    }

    public async Task<Unit> HandleAsync(UpdateUserCommand command, CancellationToken ct)
    {
        var m = command.Model;
        var user = await users.FindByIdAsync(m.Id!) ?? throw new AppException("Usuário não encontrado.");
        var deactivated = user.IsActive && !m.IsActive;

        user.FullName = m.FullName.Trim();
        user.JobTitle = m.JobTitle;
        user.Department = m.Department;
        user.AvatarUrl = m.AvatarUrl;
        user.IsActive = m.IsActive;

        var email = m.Email.Trim();
        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            AppException.ThrowIfFailed(await users.SetEmailAsync(user, email));
            AppException.ThrowIfFailed(await users.SetUserNameAsync(user, email));
        }
        AppException.ThrowIfFailed(await users.UpdateAsync(user));

        if (!string.IsNullOrWhiteSpace(m.Password))
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            AppException.ThrowIfFailed(await users.ResetPasswordAsync(user, token, m.Password));
        }
        if (deactivated) await users.UpdateSecurityStampAsync(user); // derruba sessões ativas

        await SyncRolesAsync(user, m.RoleIds, ct);
        await SyncTeamsAsync(user.Id, m.TeamIds, ct);
        return Unit.Value;
    }

    public async Task<Unit> HandleAsync(SetUsersActiveCommand command, CancellationToken ct)
    {
        foreach (var id in command.Ids)
        {
            var user = await users.FindByIdAsync(id);
            if (user is null || user.IsActive == command.Active) continue;
            user.IsActive = command.Active;
            AppException.ThrowIfFailed(await users.UpdateAsync(user));
            if (!command.Active) await users.UpdateSecurityStampAsync(user);
        }
        return Unit.Value;
    }

    public async Task<Unit> HandleAsync(DeleteUsersCommand command, CancellationToken ct)
    {
        foreach (var id in command.Ids)
        {
            var user = await users.FindByIdAsync(id);
            if (user is not null) AppException.ThrowIfFailed(await users.DeleteAsync(user));
        }
        return Unit.Value;
    }

    private async Task SyncRolesAsync(AppUser user, List<string> roleIds, CancellationToken ct)
    {
        var wanted = await db.Roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Name!).ToListAsync(ct);
        var current = await users.GetRolesAsync(user);
        var remove = current.Except(wanted).ToList();
        var add = wanted.Except(current).ToList();
        if (remove.Count > 0) AppException.ThrowIfFailed(await users.RemoveFromRolesAsync(user, remove));
        if (add.Count > 0) AppException.ThrowIfFailed(await users.AddToRolesAsync(user, add));
    }

    private async Task SyncTeamsAsync(string userId, List<int> teamIds, CancellationToken ct)
    {
        var current = await db.TeamMembers.Where(tm => tm.UserId == userId).ToListAsync(ct);
        db.TeamMembers.RemoveRange(current.Where(c => !teamIds.Contains(c.TeamId)));
        foreach (var id in teamIds.Except(current.Select(c => c.TeamId)))
            db.TeamMembers.Add(new TeamMember { TeamId = id, UserId = userId, Role = TeamRole.Member });
        await db.SaveChangesAsync(ct);
    }
}
