using System.Globalization;
using System.Text;
using BlazorApp3.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Data;

/// <summary>
/// Aplica as migrations e popula dados para testar todos os fluxos.
/// Para recriar do zero basta apagar o arquivo app.db.
/// </summary>
public static class DbSeeder
{
    public const string AdminRoleName = "Administrador";

    public sealed record DemoAccount(string Email, string Password, string Name, string Role, string Department, string JobTitle, bool Active = true);

    public static readonly IReadOnlyList<DemoAccount> DemoAccounts =
    [
        new("admin@demo.com",    "Admin@123",    "Ana Administradora", AdminRoleName,  "Tecnologia", "CTO"),
        new("gestor@demo.com",   "Gestor@123",   "Gustavo Gestor",     "Gestor",       "Vendas",     "Gerente Comercial"),
        new("analista@demo.com", "Analista@123", "Alice Analista",     "Analista",     "Financeiro", "Analista Financeira"),
        new("suporte@demo.com",  "Suporte@123",  "Sérgio Suporte",     "Suporte",      "Atendimento","Coordenador de Suporte"),
        new("designer@demo.com", "Designer@123", "Débora Designer",    "Designer",     "Marketing",  "Product Designer"),
        new("viewer@demo.com",   "Viewer@123",   "Vitor Visualizador", "Visualizador", "Operações",  "Estagiário"),
        new("inativo@demo.com",  "Inativo@123",  "Igor Inativo",       "Analista",     "Jurídico",   "Advogado", Active: false),
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        // Aplica migrations pendentes (cria o banco se não existir)
        await db.Database.MigrateAsync();

        await SyncPermissionsAsync(db);

        if (!await db.BrandingSettings.AnyAsync())
        {
            db.BrandingSettings.Add(BrandingSettings.CreateDefault());
            await db.SaveChangesAsync();
        }

        if (!await db.SystemSettings.AnyAsync())
        {
            db.SystemSettings.Add(new SystemSettings());
            await db.SaveChangesAsync();
        }

        var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
        if (!await db.Roles.AnyAsync())
            await SeedRolesAsync(db, roleManager);

        await EnsureAdminHasAllPermissionsAsync(db, roleManager);

        if (!await db.Users.AnyAsync())
        {
            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            await SeedUsersAndTeamsAsync(db, userManager);
            logger.LogInformation("Seed concluído: {Users} usuários, {Teams} times.", await db.Users.CountAsync(), await db.Teams.CountAsync());
        }
    }

    /// <summary>Mantém a tabela Permissions idêntica ao catálogo fixo em código.</summary>
    private static async Task SyncPermissionsAsync(AppDbContext db)
    {
        var existing = await db.Permissions.ToDictionaryAsync(p => p.Id);
        foreach (var def in Perm.All)
        {
            if (existing.Remove(def.Id, out var p))
            {
                p.Name = def.Name; p.Group = def.Group; p.Description = def.Description;
            }
            else
            {
                db.Permissions.Add(new Permission { Id = def.Id, Name = def.Name, Group = def.Group, Description = def.Description });
            }
        }
        db.Permissions.RemoveRange(existing.Values); // permissões removidas do código
        await db.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(AppDbContext db, RoleManager<AppRole> roleManager)
    {
        var roles = new (string Name, string Description, string Color, bool System, string[] Perms)[]
        {
            (AdminRoleName, "Acesso total ao sistema", "#dc2626", true, Perm.All.Select(p => p.Name).ToArray()),
            ("Gestor", "Gerencia usuários e times", "#2563eb", false,
                [Perm.DashboardView, Perm.UsersView, Perm.UsersCreate, Perm.UsersEdit, Perm.UsersBlock, Perm.TeamsView, Perm.TeamsCreate,
                 Perm.TeamsEdit, Perm.TeamsDelete, Perm.TeamsManageMembers, Perm.RolesView, Perm.PermissionsView, Perm.BrandingView]),
            ("Analista", "Consulta de dados", "#059669", false,
                [Perm.DashboardView, Perm.UsersView, Perm.TeamsView, Perm.PermissionsView]),
            ("Suporte", "Atendimento a usuários", "#d97706", false,
                [Perm.DashboardView, Perm.UsersView, Perm.UsersEdit, Perm.TeamsView, Perm.TeamsManageMembers]),
            ("Designer", "Identidade visual e whitelabel", "#db2777", false,
                [Perm.DashboardView, Perm.BrandingView, Perm.BrandingEdit, Perm.TeamsView, Perm.TeamsEdit, Perm.ComponentsView]),
            ("Visualizador", "Somente o dashboard", "#64748b", false, [Perm.DashboardView]),
            ("Convidado", "Sem permissões (teste de acesso negado)", "#94a3b8", false, []),
        };

        var permIds = await db.Permissions.ToDictionaryAsync(p => p.Name, p => p.Id);
        var date = DateTime.UtcNow.AddDays(-400);
        foreach (var (name, desc, color, system, perms) in roles)
        {
            var role = new AppRole(name) { Description = desc, Color = color, IsSystem = system, CreatedAt = date = date.AddDays(17) };
            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            db.RolePermissions.AddRange(perms.Select(p => new RolePermission { RoleId = role.Id, PermissionId = permIds[p] }));
        }
        await db.SaveChangesAsync();
    }

    private static async Task EnsureAdminHasAllPermissionsAsync(AppDbContext db, RoleManager<AppRole> roleManager)
    {
        var admin = await roleManager.FindByNameAsync(AdminRoleName);
        if (admin is null) return;
        var has = await db.RolePermissions.Where(rp => rp.RoleId == admin.Id).Select(rp => rp.PermissionId).ToListAsync();
        var missing = Perm.All.Select(p => p.Id).Except(has).ToList();
        if (missing.Count == 0) return;
        db.RolePermissions.AddRange(missing.Select(id => new RolePermission { RoleId = admin.Id, PermissionId = id }));
        await db.SaveChangesAsync();
    }

    private static readonly string[] FirstNames =
    [
        "Lucas", "Mariana", "Pedro", "Juliana", "Rafael", "Camila", "Bruno", "Fernanda", "Thiago", "Larissa", "Diego", "Beatriz",
        "Felipe", "Isabela", "Gabriel", "Letícia", "Mateus", "Amanda", "Rodrigo", "Patrícia", "Gustavo", "Natália", "Eduardo",
        "Carolina", "André", "Bianca", "Leonardo", "Vanessa", "Henrique", "Priscila", "João", "Renata", "Caio", "Tatiane",
        "Vinícius", "Aline", "Marcelo", "Daniela", "Otávio", "Helena", "Samuel", "Lívia", "Igor", "Sofia", "Fábio", "Luana"
    ];

    private static readonly string[] LastNames =
    [
        "Silva", "Santos", "Oliveira", "Souza", "Rodrigues", "Ferreira", "Alves", "Pereira", "Lima", "Gomes", "Costa", "Ribeiro",
        "Martins", "Carvalho", "Almeida", "Lopes", "Soares", "Fernandes", "Vieira", "Barbosa", "Rocha", "Dias", "Nascimento",
        "Andrade", "Moreira", "Nunes", "Marques", "Machado", "Mendes", "Freitas", "Cardoso", "Ramos", "Gonçalves", "Teixeira"
    ];

    private static readonly Dictionary<string, string[]> Departments = new()
    {
        ["Tecnologia"] = ["Desenvolvedor Backend", "Desenvolvedora Frontend", "Engenheiro de Dados", "Tech Lead", "QA Analyst", "DevOps Engineer"],
        ["Financeiro"] = ["Analista Financeiro", "Controller", "Tesoureiro", "Assistente Contábil"],
        ["Marketing"] = ["Analista de Marketing", "Social Media", "Product Designer", "Growth Hacker"],
        ["Vendas"] = ["Executivo de Contas", "SDR", "Gerente de Vendas", "Customer Success"],
        ["RH"] = ["Business Partner", "Recrutadora", "Analista de DP"],
        ["Operações"] = ["Analista de Operações", "Coordenador de Logística", "Supervisor"],
        ["Jurídico"] = ["Advogado", "Paralegal"],
        ["Atendimento"] = ["Atendente", "Analista de Suporte N2", "Coordenador de Suporte"],
    };

    private static async Task SeedUsersAndTeamsAsync(AppDbContext db, UserManager<AppUser> userManager)
    {
        var rnd = new Random(42);
        var now = DateTime.UtcNow;
        var roles = await db.Roles.ToDictionaryAsync(r => r.Name!, r => r.Id);

        // 1) Contas demo (via UserManager para validar senha)
        var demoUsers = new Dictionary<string, AppUser>();
        foreach (var d in DemoAccounts)
        {
            var user = new AppUser
            {
                UserName = d.Email, Email = d.Email, EmailConfirmed = true, FullName = d.Name,
                Department = d.Department, JobTitle = d.JobTitle, IsActive = d.Active,
                CreatedAt = now.AddDays(-500 + demoUsers.Count), LastLoginAt = d.Active ? now.AddHours(-rnd.Next(1, 72)) : null
            };
            var result = await userManager.CreateAsync(user, d.Password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(user, d.Role);
            demoUsers[d.Email] = user;
        }

        // 2) Usuários gerados em massa (insert direto, hash de senha calculado uma única vez)
        var passwordHash = userManager.PasswordHasher.HashPassword(new AppUser(), "Demo@123");
        var assignableRoles = new[] { "Gestor", "Analista", "Analista", "Analista", "Suporte", "Suporte", "Designer", "Visualizador", "Visualizador", "Convidado" };
        var generated = new List<AppUser>();
        var userRoles = new List<IdentityUserRole<string>>();
        var usedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < 180; i++)
        {
            var first = FirstNames[rnd.Next(FirstNames.Length)];
            var last = LastNames[rnd.Next(LastNames.Length)];
            var last2 = LastNames[rnd.Next(LastNames.Length)];
            var baseEmail = $"{Slug(first)}.{Slug(last)}";
            var email = $"{baseEmail}@empresa.com.br";
            for (var n = 2; !usedEmails.Add(email); n++) email = $"{baseEmail}{n}@empresa.com.br";

            var dept = Departments.Keys.ElementAt(rnd.Next(Departments.Count));
            var titles = Departments[dept];
            var created = now.AddDays(-rnd.Next(1, 540)).AddMinutes(-rnd.Next(0, 1440));
            var active = rnd.NextDouble() > 0.15;

            var user = new AppUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = email, NormalizedUserName = email.ToUpperInvariant(),
                Email = email, NormalizedEmail = email.ToUpperInvariant(), EmailConfirmed = true,
                PasswordHash = passwordHash,
                SecurityStamp = Guid.NewGuid().ToString("N").ToUpperInvariant(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                LockoutEnabled = true,
                FullName = $"{first} {last} {last2}",
                Department = dept,
                JobTitle = titles[rnd.Next(titles.Length)],
                IsActive = active,
                CreatedAt = created,
                LastLoginAt = active && rnd.NextDouble() > 0.2 ? created.AddDays(rnd.Next(0, Math.Max(1, (int)(now - created).TotalDays))) : null
            };
            generated.Add(user);

            if (rnd.NextDouble() > 0.05)
            {
                var roleName = assignableRoles[rnd.Next(assignableRoles.Length)];
                userRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = roles[roleName] });
                if (rnd.NextDouble() < 0.1)
                {
                    var second = assignableRoles[rnd.Next(assignableRoles.Length)];
                    if (second != roleName) userRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = roles[second] });
                }
            }
        }
        db.Users.AddRange(generated);
        db.UserRoles.AddRange(userRoles);
        await db.SaveChangesAsync();

        // 3) Times (alguns com branding personalizado)
        var teams = new (string Name, string? Display, string Color, string? Accent, string? Sidebar, bool Custom, bool Active, string Desc, string[] Depts)[]
        {
            ("Plataforma", "Plataforma Labs", "#7c3aed", "#22d3ee", "#1e1b4b", true, true, "Time responsável pela plataforma core e infraestrutura.", ["Tecnologia"]),
            ("Growth", "Growth Hub", "#db2777", "#f59e0b", "#3b0a24", true, true, "Aquisição, ativação e retenção de clientes.", ["Marketing", "Vendas"]),
            ("Financeiro Corporativo", "FinOps", "#059669", "#84cc16", "#052e2b", true, true, "Planejamento financeiro, contas a pagar e receber.", ["Financeiro"]),
            ("Atendimento ao Cliente", "Central do Cliente", "#0891b2", "#f97316", "#082f49", true, true, "Suporte N1/N2 e sucesso do cliente.", ["Atendimento"]),
            ("Vendas Enterprise", null, "#ea580c", null, null, false, true, "Contas estratégicas e grandes clientes.", ["Vendas"]),
            ("Pessoas & Cultura", null, "#e11d48", null, null, false, true, "Recrutamento, onboarding e cultura.", ["RH"]),
            ("Data & Analytics", "DataLab", "#0d9488", "#a855f7", "#042f2e", true, true, "BI, ciência de dados e governança.", ["Tecnologia", "Financeiro"]),
            ("Jurídico", null, "#475569", null, null, false, true, "Contratos, compliance e LGPD.", ["Jurídico"]),
            ("Operações Norte", null, "#ca8a04", null, null, false, false, "Operação regional (desativado).", ["Operações"]),
            ("Squad Mobile", "Mobile Squad", "#2563eb", "#10b981", null, true, true, "Aplicativos iOS e Android.", ["Tecnologia", "Marketing"]),
        };

        var allUsers = generated.Concat(demoUsers.Values).ToList();
        var teamEntities = new List<Team>();
        var created2 = now.AddDays(-380);
        foreach (var t in teams)
        {
            var team = new Team
            {
                Name = t.Name, Slug = Slug(t.Name), Description = t.Desc, IsActive = t.Active, CreatedAt = created2 = created2.AddDays(rnd.Next(5, 30)),
                UseCustomBranding = t.Custom, DisplayName = t.Display, PrimaryColor = t.Color, AccentColor = t.Accent, SidebarColor = t.Sidebar
            };
            var candidates = allUsers.Where(u => t.Depts.Contains(u.Department)).OrderBy(_ => rnd.Next()).Take(rnd.Next(10, 28)).ToList();
            candidates.AddRange(allUsers.Where(u => !t.Depts.Contains(u.Department)).OrderBy(_ => rnd.Next()).Take(rnd.Next(1, 5)));
            for (var i = 0; i < candidates.Count; i++)
            {
                team.Members.Add(new TeamMember
                {
                    UserId = candidates[i].Id,
                    Role = i == 0 ? TeamRole.Owner : i <= 2 ? TeamRole.Admin : TeamRole.Member,
                    JoinedAt = team.CreatedAt.AddDays(rnd.Next(0, 300))
                });
            }
            teamEntities.Add(team);
        }

        // Garante vínculos das contas demo para testar troca de time / branding
        void Ensure(string team, string email, TeamRole role)
        {
            var tm = teamEntities.First(t => t.Name == team);
            var uid = demoUsers[email].Id;
            var existing = tm.Members.FirstOrDefault(m => m.UserId == uid);
            if (existing is null) tm.Members.Add(new TeamMember { UserId = uid, Role = role, JoinedAt = tm.CreatedAt });
            else existing.Role = role;
        }
        Ensure("Plataforma", "admin@demo.com", TeamRole.Owner);
        Ensure("Growth", "admin@demo.com", TeamRole.Admin);
        Ensure("Data & Analytics", "admin@demo.com", TeamRole.Member);
        Ensure("Financeiro Corporativo", "admin@demo.com", TeamRole.Member);
        Ensure("Vendas Enterprise", "gestor@demo.com", TeamRole.Owner);
        Ensure("Atendimento ao Cliente", "gestor@demo.com", TeamRole.Admin);
        Ensure("Financeiro Corporativo", "analista@demo.com", TeamRole.Member);
        Ensure("Atendimento ao Cliente", "suporte@demo.com", TeamRole.Owner);
        Ensure("Growth", "designer@demo.com", TeamRole.Member);
        Ensure("Squad Mobile", "designer@demo.com", TeamRole.Admin);
        Ensure("Operações Norte", "viewer@demo.com", TeamRole.Member);

        db.Teams.AddRange(teamEntities);
        await db.SaveChangesAsync();
    }

    public static string Slug(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            else if (sb.Length > 0 && sb[^1] != '-') sb.Append('-');
        }
        return sb.ToString().Trim('-');
    }
}
