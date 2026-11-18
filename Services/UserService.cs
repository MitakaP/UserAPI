using UserAPI.DTOs;
using UserAPI.Interfaces;
using UserAPI.Models;
using System.Security.Cryptography;
using System.Text;

namespace UserAPI.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordService _passwordService;
    private readonly TokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IMfaService _mfaService;

    public UserService(
        IUserRepository userRepository,
        PasswordService passwordService,
        TokenService tokenService,
        IEmailService emailService,
        IMfaService mfaService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _emailService = emailService;
        _mfaService = mfaService;
    }

    public async Task<List<UserResponseDto>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();

        return users.Select(user => new UserResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        }).ToList();
    }

    public async Task<UserResponseDto?> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
            return null;

        return new UserResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserResponseDto?> CreateAsync(CreateUserDto dto)
    {
        var emailExists = await _userRepository.EmailExistsAsync(dto.Email);

        if (emailExists)
            return null;

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email
        };

        user.Password = _passwordService.HashPassword(user, dto.Password);

        await _userRepository.AddAsync(user);

        return new UserResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);

        if (user == null)
            return null;

        var isPasswordValid = _passwordService.VerifyPassword(
            user,
            user.Password,
            dto.Password);

        if (!isPasswordValid)
            return null;

        // Ако MFA е активирана, не издаваме JWT на този етап.
        if (user.IsMfaEnabled)
        {
            return new LoginResponseDto
            {
                RequiresMfa = true
            };
        }

        // Ако MFA не е активирана, Login приключва нормално.
        var token = _tokenService.GenerateToken(user);

        return new LoginResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            RequiresMfa = false,
            Token = token
        };
    }

    public async Task<LoginResponseDto?> VerifyMfaLoginAsync(
    LoginMfaDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);

        if (user == null || !user.IsMfaEnabled)
            return null;

        // Проверяваме паролата отново.
        var isPasswordValid = _passwordService.VerifyPassword(
            user,
            user.Password,
            dto.Password);

        if (!isPasswordValid)
            return null;

        // Проверяваме дали има запазен MFA секрет.
        if (string.IsNullOrWhiteSpace(user.MfaSecretProtected))
            return null;

        // Разкодираме защитения секрет.
        var secret = _mfaService.UnprotectSecret(
            user.MfaSecretProtected);

        // Проверяваме кода от Google Authenticator.
        var isCodeValid = _mfaService.VerifyCode(
            secret,
            dto.Code);

        if (!isCodeValid)
            return null;

        // Издаваме JWT само след успешни проверки.
        var token = _tokenService.GenerateToken(user);

        return new LoginResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            RequiresMfa = false,
            Token = token
        };
    }

    public async Task<UpdateUserResult> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
            return UpdateUserResult.NotFound;

        var emailExists = await _userRepository.EmailExistsAsync(dto.Email, id);

        if (emailExists)
            return UpdateUserResult.EmailAlreadyExists;

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Email = dto.Email;

        await _userRepository.UpdateAsync(user);

        return UpdateUserResult.Success;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
            return false;

        await _userRepository.DeleteAsync(user);

        return true;
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);

        // Не разкриваме дали този имейл съществува.
        if (user == null)
            return;

        // Генерираме случаен токен.
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        // Запазваме само SHA-256 хеша.
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        user.PasswordResetTokenHash = tokenHash;
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);

        await _userRepository.UpdateAsync(user);

        var subject = "UserAPI - Password Reset";

        var body = $"""
        Hello {user.FirstName},

        We received a request to reset your password.

        Your password reset token is:

        {token}

        This token expires in 15 minutes.

        If you did not request this, ignore this email.
        """;

        await _emailService.SendEmailAsync(
            user.Email,
            subject,
            body);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(dto.Token)));

        var user = await _userRepository
            .GetByPasswordResetTokenHashAsync(tokenHash);

        if (user == null)
            return false;

        if (user.Email != dto.Email)
            return false;

        if (user.PasswordResetTokenExpiresAt == null ||
            user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            return false;

        // Задаваме новата парола с хеширане.
        user.Password = _passwordService.HashPassword(
            user,
            dto.NewPassword);

        // Използваният токен вече не е валиден.
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAt = null;

        await _userRepository.UpdateAsync(user);

        return true;
    }

    public async Task<MfaSetupResponseDto?> SetupMfaAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null || user.IsMfaEnabled)
            return null;

        var secret = _mfaService.GenerateSecret();

        user.MfaSecretProtected = _mfaService.ProtectSecret(secret);
        user.MfaSetupExpiresAt = DateTime.UtcNow.AddMinutes(10);

        await _userRepository.UpdateAsync(user);

        return new MfaSetupResponseDto
        {
            QrCodeImage = _mfaService.GenerateQrCode(user.Email, secret),
            ManualEntryKey = secret
        };
    }

    public async Task<bool> VerifyMfaSetupAsync(
        int userId,
        VerifyMfaDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null ||
            user.IsMfaEnabled ||
            user.MfaSecretProtected == null ||
            user.MfaSetupExpiresAt == null ||
            user.MfaSetupExpiresAt <= DateTime.UtcNow)
        {
            return false;
        }

        var secret = _mfaService.UnprotectSecret(
            user.MfaSecretProtected);

        var isValid = _mfaService.VerifyCode(secret, dto.Code);

        if (!isValid)
            return false;

        user.IsMfaEnabled = true;
        user.MfaSetupExpiresAt = null;

        await _userRepository.UpdateAsync(user);

        return true;
    }
}
