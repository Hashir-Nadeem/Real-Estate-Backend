using BCrypt.Net;
using Real_Estate_WebAPI.DTOs.Auth;
using Real_Estate_WebAPI.Interfaces;
using Real_Estate_WebAPI.Models;
using Real_Estate_WebAPI.Services.Email;
using System.Security.Cryptography;
using static System.Net.WebRequestMethods;


namespace Real_Estate_WebAPI.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _users;
        private readonly ITokenService _tokens;
        private readonly IHttpContextAccessor _http;
        private readonly IEmailService _email;

        public AuthService(
            IUserRepository users,
            ITokenService tokens,
            IHttpContextAccessor http,
            IEmailService email)
        {
            _users = users;
            _tokens = tokens;
            _http = http;
            _email = email;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Check duplicates FIRST
            if (await _users.ExistsByEmailAsync(request.Email))
                throw new Exception("Email already registered.");

            if (await _users.ExistsByPhoneAsync(request.PhoneNumber))
                throw new Exception("Phone already registered.");

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email.ToLowerInvariant().Trim(),
                PhoneNumber = request.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = "User"
            };

            var refreshToken = _tokens.GenerateRefreshToken(request.RememberMe);
            refreshToken.IpAddress = _http.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            refreshToken.Device =
                _http.HttpContext?.Request.Headers["User-Agent"].ToString();


            user.RefreshTokens.Add(refreshToken);
            await _users.CreateAsync(user);

            var accessToken = _tokens.GenerateAccessToken(user);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                AccessTokenExpiresAt = refreshToken.ExpiresAt
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var normalized = request.EmailOrPhone.ToLowerInvariant().Trim();

            var user =
                await _users.GetByEmailAsync(normalized)
                ?? await _users.GetByPhoneAsync(request.EmailOrPhone);

            if (user == null)
                throw new Exception("Invalid credentials.");

            var validPassword = BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash);

            if (!validPassword)
                throw new Exception("Invalid credentials.");

            var refreshToken = _tokens.GenerateRefreshToken(request.RememberMe);


            user.RefreshTokens.Add(refreshToken);

            await _users.UpdateAsync(user);

            var accessToken = _tokens.GenerateAccessToken(user);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                AccessTokenExpiresAt = refreshToken.ExpiresAt
            };
        }

        public async Task ForgotPasswordAsync(string email)
        {
            var user = await _users.GetByEmailAsync(email.ToLower());

            // IMPORTANT — do NOT reveal user existence
            if (user == null)
                return;

            var token = GeneratePasswordResetToken();

            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiresAt =
                DateTime.UtcNow.AddHours(1);

            await _users.UpdateAsync(user);

            var resetLink =
                $"https://yourfrontend.com/reset-password?token={token}";

            await _email.SendAsync(
                user.Email,
                "Reset Your Password",
                $"Click here to reset: <a href='{resetLink}'>Reset Password</a>");
        }

        public async Task ResetPasswordAsync(
     string token,
     string newPassword)
        {
            // 🔥 HASH TOKEN HERE
            using var sha = System.Security.Cryptography.SHA256.Create();

            var hashedBytes = sha.ComputeHash(
                System.Text.Encoding.UTF8.GetBytes(token));

            var hashedToken = Convert.ToBase64String(hashedBytes);

            var user = await _users.GetByResetTokenAsync(hashedToken);

            if (user == null ||
                user.PasswordResetTokenExpiresAt <= DateTime.UtcNow)
            {
                throw new Exception("Invalid or expired token.");
            }

            // ✅ Hash password
            user.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(newPassword);

            // ✅ Destroy reset token
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiresAt = null;

            // 🔥 SECURITY: revoke ALL sessions
            user.RefreshTokens?.ForEach(t =>
            {
                t.IsRevoked = true;
                t.RevokedReason = "Password reset";
            });

            await _users.UpdateAsync(user);
        }



        private string GeneratePasswordResetToken()
        {
            return Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(64));
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var user = await _users.GetUserByRefreshTokenAsync(refreshToken);

            if (user == null)
                throw new Exception("Invalid refresh token");

            var token = user.RefreshTokens
                .Single(x => x.Token == refreshToken);

            // 🔥 Detect reuse attack
            if (token.IsRevoked)
            {
                // BREACH DETECTED → kill all sessions
                user.RefreshTokens.ForEach(t =>
                {
                    t.IsRevoked = true;
                    t.RevokedReason = "Token reuse detected";
                });

                await _users.UpdateAsync(user);

                throw new Exception("Token reuse detected. All sessions revoked.");
            }

            if (token.ExpiresAt < DateTime.UtcNow)
                throw new Exception("Refresh token expired");

            // ✅ ROTATE
            var newRefreshToken =
                _tokens.GenerateRefreshToken(false);

            token.IsRevoked = true;
            token.ReplacedByToken = newRefreshToken.Token;
            token.RevokedReason = "Rotated";

            user.RefreshTokens.Add(newRefreshToken);

            await _users.UpdateAsync(user);

            var newAccessToken =
                _tokens.GenerateAccessToken(user);

            return new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                AccessTokenExpiresAt = newRefreshToken.ExpiresAt
            };
        }

    }

}
