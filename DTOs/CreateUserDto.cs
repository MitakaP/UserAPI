using System.ComponentModel.DataAnnotations;

namespace UserAPI.DTOs;

public class CreateUserDto
{
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(4)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}
