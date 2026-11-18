namespace UserAPI.DTOs;

public class MfaSetupResponseDto
{
    public string QrCodeImage { get; set; } = string.Empty;
    public string ManualEntryKey { get; set; } = string.Empty;
}