using Microsoft.AspNetCore.DataProtection;
using OtpNet;
using QRCoder;
using UserAPI.Interfaces;

namespace UserAPI.Services;

public class MfaService : IMfaService
{
    private readonly IDataProtector _protector;

    public MfaService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("UserAPI.MfaSecret.v1");
    }

    public string GenerateSecret()
    {
        var secret = KeyGeneration.GenerateRandomKey(20);

        return Base32Encoding.ToString(secret);
    }

    public string ProtectSecret(string secret)
    {
        return _protector.Protect(secret);
    }

    public string UnprotectSecret(string protectedSecret)
    {
        return _protector.Unprotect(protectedSecret);
    }

    public string GenerateQrCode(string email, string secret)
    {
        var issuer = "UserAPI";
        var account = Uri.EscapeDataString($"{issuer}:{email}");

        var uri =
            $"otpauth://totp/{account}" +
            $"?secret={secret}" +
            $"&issuer={Uri.EscapeDataString(issuer)}" +
            "&digits=6&period=30&algorithm=SHA1";

        using var qrGenerator = new QRCodeGenerator();

        using var qrCodeData = qrGenerator.CreateQrCode(
            uri,
            QRCodeGenerator.ECCLevel.Q);

        var qrCode = new PngByteQRCode(qrCodeData);

        var pngBytes = qrCode.GetGraphic(10);

        return $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
    }

    public bool VerifyCode(string secret, string code)
    {
        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);

            var totp = new Totp(secretBytes);

            return totp.VerifyTotp(
                code,
                out _,
                new VerificationWindow(previous: 1, future: 1));
        }
        catch
        {
            return false;
        }
    }
}
