using System.ComponentModel.DataAnnotations;

namespace UserAPI.DTOs;

public class VerifyMfaDto
{
    [Required]
    [RegularExpression(@"^\d{6}$")]
    public string Code { get; set; } = string.Empty;
}