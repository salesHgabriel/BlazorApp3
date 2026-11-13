using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BlazorApp3.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, string>(options)
{
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<BrandingSettings> BrandingSettings => Set<BrandingSettings>();
    public DbSet<EmailConfirmationCode> EmailConfirmationCodes => Set<EmailConfirmationCode>();
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<UserConnection> UserConnections => Set<UserConnection>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.FullName);
            e.HasIndex(u => u.Department);
            // SQLite não compara DateTimeOffset em SQL: grava como inteiro ordenável (permite filtrar bloqueados)
            e.Property(u => u.LockoutEnd).HasConversion(new DateTimeOffsetToBinaryConverter());
        });

        builder.Entity<EmailConfirmationCode>(e =>
        {
            e.HasIndex(c => new { c.UserId, c.CreatedAt });
            e.HasIndex(c => c.ExpiresAt);
            e.HasOne(c => c.User).WithMany(u => u.ConfirmationCodes).HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Permission>(e =>
        {
            e.Property(p => p.Id).ValueGeneratedNever(); // Ids fixos
            e.HasIndex(p => p.Name).IsUnique();
        });

        builder.Entity<RolePermission>(e =>
        {
            e.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            e.HasOne(rp => rp.Role).WithMany(r => r.Permissions).HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(rp => rp.Permission).WithMany(p => p.Roles).HasForeignKey(rp => rp.PermissionId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Team>(e =>
        {
            e.HasIndex(t => t.Slug).IsUnique();
            e.HasIndex(t => t.Name);
        });

        builder.Entity<TeamMember>(e =>
        {
            e.HasKey(tm => new { tm.TeamId, tm.UserId });
            e.HasOne(tm => tm.Team).WithMany(t => t.Members).HasForeignKey(tm => tm.TeamId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(tm => tm.User).WithMany(u => u.Teams).HasForeignKey(tm => tm.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BrandingSettings>(e => e.Property(b => b.Id).ValueGeneratedNever());
        builder.Entity<SystemSettings>(e => e.Property(s => s.Id).ValueGeneratedNever());

        builder.Entity<Notification>(e =>
        {
            e.HasIndex(n => new { n.UserId, n.CreatedAt });
            e.HasIndex(n => new { n.UserId, n.ReadAt });
            e.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserSession>(e =>
        {
            e.HasIndex(s => new { s.UserId, s.RevokedAt });
            e.HasIndex(s => s.LastSeenAt);
            e.HasOne(s => s.User).WithMany(u => u.Sessions).HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserConnection>(e =>
        {
            e.HasKey(c => c.ConnectionId);
            e.HasIndex(c => c.SessionId);
            e.HasOne(c => c.Session).WithMany(s => s.Connections).HasForeignKey(c => c.SessionId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
