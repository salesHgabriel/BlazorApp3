namespace BlazorApp3.Security;

public sealed record PermissionDefinition(int Id, string Name, string Group, string Description);

/// <summary>
/// Catálogo de permissões FIXAS (Id e nome imutáveis). Sincronizado com o banco na inicialização.
/// Roles são dinâmicas e apenas agrupam estas permissões.
/// </summary>
public static class Perm
{
    public const string PolicyPrefix = "perm:";
    public const string ClaimType = "permission";

    public const string DashboardView = "Dashboard.View";

    public const string UsersView = "Users.View";
    public const string UsersCreate = "Users.Create";
    public const string UsersEdit = "Users.Edit";
    public const string UsersDelete = "Users.Delete";

    public const string RolesView = "Roles.View";
    public const string RolesCreate = "Roles.Create";
    public const string RolesEdit = "Roles.Edit";
    public const string RolesDelete = "Roles.Delete";

    public const string PermissionsView = "Permissions.View";

    public const string TeamsView = "Teams.View";
    public const string TeamsCreate = "Teams.Create";
    public const string TeamsEdit = "Teams.Edit";
    public const string TeamsDelete = "Teams.Delete";
    public const string TeamsManageMembers = "Teams.ManageMembers";

    public const string BrandingView = "Branding.View";
    public const string BrandingEdit = "Branding.Edit";

    public const string ComponentsView = "Components.View";

    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        new(1,  DashboardView,      "Dashboard",   "Visualizar o dashboard"),

        new(10, UsersView,          "Usuários",    "Listar e visualizar usuários"),
        new(11, UsersCreate,        "Usuários",    "Criar usuários"),
        new(12, UsersEdit,          "Usuários",    "Editar usuários, ativar/inativar"),
        new(13, UsersDelete,        "Usuários",    "Excluir usuários"),

        new(20, RolesView,          "Papéis",      "Listar e visualizar papéis (roles)"),
        new(21, RolesCreate,        "Papéis",      "Criar papéis"),
        new(22, RolesEdit,          "Papéis",      "Editar papéis e suas permissões"),
        new(23, RolesDelete,        "Papéis",      "Excluir papéis"),

        new(25, PermissionsView,    "Papéis",      "Visualizar catálogo de permissões"),

        new(30, TeamsView,          "Times",       "Listar e visualizar times"),
        new(31, TeamsCreate,        "Times",       "Criar times"),
        new(32, TeamsEdit,          "Times",       "Editar times e branding do time"),
        new(33, TeamsDelete,        "Times",       "Excluir times"),
        new(34, TeamsManageMembers, "Times",       "Adicionar/remover membros"),

        new(40, BrandingView,       "Whitelabel",  "Visualizar configurações whitelabel"),
        new(41, BrandingEdit,       "Whitelabel",  "Alterar configurações whitelabel"),

        new(50, ComponentsView,     "Sistema",     "Acessar a vitrine de componentes")
    ];

    public static string Policy(string permission) => PolicyPrefix + permission;

    /// <summary>Policy que é satisfeita por QUALQUER uma das permissões.</summary>
    public static string AnyPolicy(params string[] permissions) => PolicyPrefix + string.Join('|', permissions);
}
