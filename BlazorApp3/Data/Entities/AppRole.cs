using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp3.Data;

/// <summary>Role criada dinamicamente. Agrupa permissões fixas.</summary>
public class AppRole : IdentityRole
{
    public AppRole() { }
    public AppRole(string name) : base(name) { }

    [MaxLength(300)] public string? Description { get; set; }
    [MaxLength(9)] public string Color { get; set; } = "#6366f1";

    /// <summary>Roles de sistema não podem ser excluídas nem renomeadas.</summary>
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RolePermission> Permissions { get; set; } = [];
}
