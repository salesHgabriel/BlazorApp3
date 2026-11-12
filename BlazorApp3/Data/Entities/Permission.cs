using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp3.Data;

/// <summary>Permissão com Id e nome fixos (definidos em <see cref="Security.Perm"/>).</summary>
public class Permission
{
    public int Id { get; set; }
    [MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(60)] public string Group { get; set; } = "";
    [MaxLength(200)] public string Description { get; set; } = "";
    public ICollection<RolePermission> Roles { get; set; } = [];
}
