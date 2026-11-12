using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Features.Users;
using BlazorApp3.Services;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Teams;

internal sealed class TeamQueryHandlers(AppDbContext db) :
    IQueryHandler<SearchTeamsQuery, PagedResult<TeamListItem>>,
    IQueryHandler<TeamSummaryQuery, TeamListItem?>,
    IQueryHandler<TeamOptionsQuery, List<SelectOption<int>>>,
    IQueryHandler<GetTeamForEditQuery, TeamEditModel?>,
    IQueryHandler<TeamMembersQuery, PagedResult<TeamMemberItem>>,
    IQueryHandler<TeamRoleCountsQuery, Dictionary<TeamRole, int>>,
    IQueryHandler<UserTeamsQuery, List<TeamOption>>,
    IQueryHandler<ActiveTeamIdQuery, int?>
{
    // Projeção reutilizada pela busca e pelo resumo
    private static IQueryable<TeamListItem> Project(IQueryable<Team> teams) => teams.Select(t => new TeamListItem
    {
        Id = t.Id, Name = t.Name, Slug = t.Slug, Description = t.Description, DisplayName = t.DisplayName, LogoUrl = t.LogoUrl,
        PrimaryColor = t.PrimaryColor, AccentColor = t.AccentColor, SidebarColor = t.SidebarColor, IsActive = t.IsActive,
        UseCustomBranding = t.UseCustomBranding, CreatedAt = t.CreatedAt, MemberCount = t.Members.Count
    });

    public async Task<PagedResult<TeamListItem>> HandleAsync(SearchTeamsQuery query, CancellationToken ct)
    {
        var q = db.Teams.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.LikePattern();
            q = q.Where(t => EF.Functions.Like(t.Name, s, "\\") || EF.Functions.Like(t.DisplayName!, s, "\\") || EF.Functions.Like(t.Description!, s, "\\"));
        }
        if (query.IsActive.HasValue) q = q.Where(t => t.IsActive == query.IsActive);
        if (query.CustomBranding.HasValue) q = q.Where(t => t.UseCustomBranding == query.CustomBranding);

        var projected = Project(q);
        projected = query.SortBy switch
        {
            "members" => projected.OrderByDescending(t => t.MemberCount),
            "created" => projected.OrderByDescending(t => t.CreatedAt),
            _ => projected.OrderBy(t => t.Name)
        };
        var page = await projected.ToPagedAsync(query.Page, query.PageSize, ct);

        var ids = page.Items.Select(t => t.Id).ToList();
        var members = await db.TeamMembers.Where(tm => ids.Contains(tm.TeamId))
            .OrderByDescending(tm => tm.Role).ThenBy(tm => tm.JoinedAt)
            .Select(tm => new { tm.TeamId, User = new UserLookup(tm.UserId, tm.User.FullName, tm.User.Email!, tm.User.AvatarUrl, tm.User.Department) })
            .ToListAsync(ct);
        foreach (var t in page.Items)
            t.TopMembers = members.Where(m => m.TeamId == t.Id).Take(5).Select(m => m.User).ToList();
        return page;
    }

    public Task<TeamListItem?> HandleAsync(TeamSummaryQuery query, CancellationToken ct) =>
        Project(db.Teams.AsNoTracking().Where(t => t.Id == query.Id)).FirstOrDefaultAsync(ct);

    public Task<List<SelectOption<int>>> HandleAsync(TeamOptionsQuery query, CancellationToken ct) =>
        db.Teams.OrderBy(t => t.Name).Select(t => new SelectOption<int>(t.Id, t.Name, t.PrimaryColor)).ToListAsync(ct);

    public Task<TeamEditModel?> HandleAsync(GetTeamForEditQuery query, CancellationToken ct) =>
        db.Teams.AsNoTracking().Where(t => t.Id == query.Id).Select(t => new TeamEditModel
        {
            Id = t.Id, Name = t.Name, Slug = t.Slug, Description = t.Description, IsActive = t.IsActive,
            UseCustomBranding = t.UseCustomBranding, DisplayName = t.DisplayName, LogoUrl = t.LogoUrl,
            PrimaryColor = t.PrimaryColor, AccentColor = t.AccentColor, SidebarColor = t.SidebarColor
        }).FirstOrDefaultAsync(ct);

    public Task<PagedResult<TeamMemberItem>> HandleAsync(TeamMembersQuery query, CancellationToken ct)
    {
        var q = db.TeamMembers.AsNoTracking().Where(tm => tm.TeamId == query.TeamId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.LikePattern();
            q = q.Where(tm => EF.Functions.Like(tm.User.FullName, s, "\\") || EF.Functions.Like(tm.User.Email!, s, "\\"));
        }
        if (query.Role.HasValue) q = q.Where(tm => tm.Role == query.Role);

        return q.OrderByDescending(tm => tm.Role).ThenBy(tm => tm.User.FullName)
            .Select(tm => new TeamMemberItem
            {
                UserId = tm.UserId, FullName = tm.User.FullName, Email = tm.User.Email!, AvatarUrl = tm.User.AvatarUrl,
                JobTitle = tm.User.JobTitle, IsActive = tm.User.IsActive, Role = tm.Role, JoinedAt = tm.JoinedAt
            }).ToPagedAsync(query.Page, query.PageSize, ct);
    }

    public Task<Dictionary<TeamRole, int>> HandleAsync(TeamRoleCountsQuery query, CancellationToken ct) =>
        db.TeamMembers.Where(tm => tm.TeamId == query.TeamId).GroupBy(tm => tm.Role)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), ct);

    public Task<List<TeamOption>> HandleAsync(UserTeamsQuery query, CancellationToken ct) =>
        db.TeamMembers.AsNoTracking()
            .Where(tm => tm.UserId == query.UserId && tm.Team.IsActive)
            .OrderBy(tm => tm.Team.Name)
            .Select(tm => new TeamOption(tm.TeamId, tm.Team.Name, tm.Team.DisplayName, tm.Team.LogoUrl, tm.Team.PrimaryColor,
                tm.Team.AccentColor, tm.Team.SidebarColor, tm.Team.UseCustomBranding, tm.Role))
            .ToListAsync(ct);

    public Task<int?> HandleAsync(ActiveTeamIdQuery query, CancellationToken ct) =>
        db.Users.Where(u => u.Id == query.UserId).Select(u => u.ActiveTeamId).FirstOrDefaultAsync(ct);
}

internal sealed class TeamCommandHandlers(AppDbContext db) :
    ICommandHandler<SaveTeamCommand, int>,
    ICommandHandler<DeleteTeamCommand, Unit>,
    ICommandHandler<AddTeamMemberCommand, Unit>,
    ICommandHandler<UpdateTeamMemberRoleCommand, Unit>,
    ICommandHandler<RemoveTeamMemberCommand, Unit>,
    ICommandHandler<SetActiveTeamCommand, Unit>
{
    public async Task<int> HandleAsync(SaveTeamCommand command, CancellationToken ct)
    {
        var m = command.Model;
        var slug = DbSeeder.Slug(string.IsNullOrWhiteSpace(m.Slug) ? m.Name : m.Slug);
        if (slug.Length == 0) throw new AppException("Não foi possível gerar um identificador para o time.");
        if (await db.Teams.AnyAsync(t => t.Slug == slug && t.Id != m.Id, ct))
            throw new AppException($"Já existe um time com o identificador '{slug}'.");

        Team team;
        if (m.Id is null)
        {
            team = new Team { CreatedAt = DateTime.UtcNow };
            db.Teams.Add(team);
        }
        else
        {
            team = await db.Teams.FindAsync([m.Id.Value], ct) ?? throw new AppException("Time não encontrado.");
        }

        team.Name = m.Name.Trim();
        team.Slug = slug;
        team.Description = m.Description;
        team.IsActive = m.IsActive;
        team.UseCustomBranding = m.UseCustomBranding;
        team.DisplayName = string.IsNullOrWhiteSpace(m.DisplayName) ? null : m.DisplayName.Trim();
        team.LogoUrl = m.LogoUrl;
        team.PrimaryColor = m.PrimaryColor;
        team.AccentColor = string.IsNullOrWhiteSpace(m.AccentColor) ? null : m.AccentColor;
        team.SidebarColor = string.IsNullOrWhiteSpace(m.SidebarColor) ? null : m.SidebarColor;
        await db.SaveChangesAsync(ct);
        return team.Id;
    }

    public async Task<Unit> HandleAsync(DeleteTeamCommand command, CancellationToken ct)
    {
        if (await db.Teams.Where(t => t.Id == command.Id).ExecuteDeleteAsync(ct) == 0)
            throw new AppException("Time não encontrado.");
        await db.Users.Where(u => u.ActiveTeamId == command.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.ActiveTeamId, (int?)null), ct);
        return Unit.Value;
    }

    public async Task<Unit> HandleAsync(AddTeamMemberCommand command, CancellationToken ct)
    {
        if (await db.TeamMembers.AnyAsync(tm => tm.TeamId == command.TeamId && tm.UserId == command.UserId, ct))
            throw new AppException("Este usuário já é membro do time.");
        db.TeamMembers.Add(new TeamMember { TeamId = command.TeamId, UserId = command.UserId, Role = command.Role, JoinedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> HandleAsync(UpdateTeamMemberRoleCommand command, CancellationToken ct)
    {
        var member = await db.TeamMembers.FindAsync([command.TeamId, command.UserId], ct) ?? throw new AppException("Membro não encontrado.");
        if (member.Role == TeamRole.Owner && command.Role != TeamRole.Owner &&
            await db.TeamMembers.CountAsync(tm => tm.TeamId == command.TeamId && tm.Role == TeamRole.Owner, ct) == 1)
            throw new AppException("O time precisa ter pelo menos um proprietário.");
        member.Role = command.Role;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> HandleAsync(RemoveTeamMemberCommand command, CancellationToken ct)
    {
        await db.TeamMembers.Where(tm => tm.TeamId == command.TeamId && tm.UserId == command.UserId).ExecuteDeleteAsync(ct);
        await db.Users.Where(u => u.Id == command.UserId && u.ActiveTeamId == command.TeamId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.ActiveTeamId, (int?)null), ct);
        return Unit.Value;
    }

    public async Task<Unit> HandleAsync(SetActiveTeamCommand command, CancellationToken ct)
    {
        await db.Users.Where(u => u.Id == command.UserId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.ActiveTeamId, command.TeamId), ct);
        return Unit.Value;
    }
}
