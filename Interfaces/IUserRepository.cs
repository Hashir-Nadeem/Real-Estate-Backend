using Real_Estate_WebAPI.Models;

namespace Real_Estate_WebAPI.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(string id);

        Task<User?> GetByEmailAsync(string email);

        Task<User?> GetByPhoneAsync(string phone);

        Task CreateAsync(User user);

        Task UpdateAsync(User user);

        Task<bool> ExistsByEmailAsync(string email);

        Task<bool> ExistsByPhoneAsync(string phone);

        Task<User?> GetByResetTokenAsync(string token);

        Task<User?> GetUserByRefreshTokenAsync(string refreshToken);


    }

}
