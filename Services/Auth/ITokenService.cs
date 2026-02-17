using Real_Estate_WebAPI.Models;

namespace Real_Estate_WebAPI.Services.Auth
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);

        RefreshToken GenerateRefreshToken(bool rememberMe);
    }

}
