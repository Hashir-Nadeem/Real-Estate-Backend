using Real_Estate_WebAPI.DTOs.Auth;

namespace Real_Estate_WebAPI.Services.Auth
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);

        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task ForgotPasswordAsync(string email);

        Task ResetPasswordAsync(string token, string newPassword);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);


    }

}
