using BlazorApp3.Localization;
using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Services;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Teams;

internal sealed class SaveTeamHandler(AppDbContext db) : ICommandHandler<SaveTeamCommand, int>
{
    public async Task<int> HandleAsync(SaveTeamCommand command, CancellationToken ct)
    {
        var m = command.Model;
        var slug = DbSeeder.Slug(string.IsNullOrWhiteSpace(m.Slug) ? m.Name : m.Slug);
        if (slug.Length == 0) throw new AppException(AppText.Get("Não foi possível gerar um identificador para o time."));
        if (await db.Teams.AnyAsync(t => t.Slug == slug && t.Id != m.Id, ct))
            throw new AppException(AppText.Format("Já existe um time com o identificador '{0}'.", slug));

        var team = await LoadOrCreateAsync(m.Id, ct);
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

    private async Task<Team> LoadOrCreateAsync(int? id, CancellationToken ct)
    {
        if (id is not null)
            return await db.Teams.FindAsync([id.Value], ct) ?? throw new AppException(AppText.Get("Time não encontrado."));

        var team = new Team { CreatedAt = DateTime.UtcNow };
        db.Teams.Add(team);
        return team;
    }
}
