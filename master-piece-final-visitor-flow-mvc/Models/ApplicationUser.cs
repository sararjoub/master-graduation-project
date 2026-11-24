using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SkillsSwap.Models;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    [MaxLength(120)] public string FullName { get; set; } = string.Empty;
    [MaxLength(120)] public string? Location { get; set; }
    [MaxLength(1000)] public string? Bio { get; set; }
    [MaxLength(500)] public string? ProfileImageUrl { get; set; }
    public bool IsProfileComplete { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
