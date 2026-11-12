using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp3.Data;

public class TeamMember
{
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
    public TeamRole Role { get; set; } = TeamRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
