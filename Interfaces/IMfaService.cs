namespace UserAPI.Interfaces;

public interface IMfaService
{
    string GenerateSecret();

    string ProtectSecret(string secret);

    string UnprotectSecret(string protectedSecret);

    string GenerateQrCode(string email, string secret);

    bool VerifyCode(string secret, string code);
}
