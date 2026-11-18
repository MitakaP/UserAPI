namespace UserAPI.DTOs;

public class LoginResponseDto
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool RequiresMfa { get; set; }

    public string? Token { get; set; }
}