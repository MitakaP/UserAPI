using UserAPI.Models;

namespace UserAPI.Interfaces;

public interface IUserRepository
{
    Task<List<User>> GetAllAsync();

    Task<User?> GetByIdAsync(int id);

    Task<User?> GetByEmailAsync(string email);

    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);

    Task AddAsync(User user);

    Task UpdateAsync(User user);

    Task DeleteAsync(User user);

    Task<User?> GetByPasswordResetTokenHashAsync(string tokenHash);

}
