using UserAPI.DTOs;

namespace UserAPI.Interfaces;

public interface IUserService
{
    Task<List<UserResponseDto>> GetAllAsync();

    Task<UserResponseDto?> GetByIdAsync(int id);

    Task<UserResponseDto?> CreateAsync(CreateUserDto dto);

    Task<LoginResponseDto?> LoginAsync(LoginDto dto);

    Task<LoginResponseDto?> VerifyMfaLoginAsync(LoginMfaDto dto);

    Task<UpdateUserResult> UpdateAsync(int id, UpdateUserDto dto);

    Task<bool> DeleteAsync(int id);

    Task ForgotPasswordAsync(ForgotPasswordDto dto);

    Task<bool> ResetPasswordAsync(ResetPasswordDto dto);

    Task<MfaSetupResponseDto?> SetupMfaAsync(int userId);

    Task<bool> VerifyMfaSetupAsync(int userId, VerifyMfaDto dto);
}
