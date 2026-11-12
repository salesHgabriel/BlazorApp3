using BlazorApp3.Data;
using BlazorApp3.Localization;

namespace BlazorApp3.Features.Teams;

/// <summary>Rótulos do papel do membro dentro do time.</summary>
public static class TeamRoles
{
    public static string Label(TeamRole role) => AppText.Get(role switch
    {
        TeamRole.Owner => "Proprietário",
        TeamRole.Admin => "Administrador",
        _ => "Membro"
    });
}
