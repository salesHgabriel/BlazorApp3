using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, string>(options)
{
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<BrandingSettings> BrandingSettings => Set<BrandingSettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.FullName);
            e.HasIndex(u => u.Department);
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
    }
}
