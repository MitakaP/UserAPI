using System.ComponentModel.DataAnnotations;

namespace UserAPI.Models;

public class User
{
    public int Id { get; set; }

    public string? PasswordResetTokenHash { get; set; }

    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    public bool IsMfaEnabled { get; set; } = false;

    public string? MfaSecretProtected { get; set; }

    public DateTime? MfaSetupExpiresAt { get; set; }


    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
