using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Real_Estate_WebAPI.DTOs.Auth;
using Real_Estate_WebAPI.Interfaces;
using Real_Estate_WebAPI.Models;
using Real_Estate_WebAPI.Repositories;
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
                Role = request.Role
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
                AccessTokenExpiresAt = refreshToken.ExpiresAt,

                User = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.PhoneNumber,
                    user.Role // ✅ IMPORTANT
                }
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

            // ✅ Get frontend URL from configuration
            var frontendUrl = "http://localhost:3000";

            var resetLink =
                $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(token)}";

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>Reset Password</title>
</head>
<body style='margin:0;padding:0;background-color:#f4f4f4;font-family:Arial,sans-serif;'>

<table width='100%' cellpadding='0' cellspacing='0' style='padding:40px 0;background-color:#f4f4f4;'>
<tr>
<td align='center'>

<table width='500' cellpadding='0' cellspacing='0' 
style='background:#ffffff;border-radius:8px;padding:40px;'>

<tr>
<td align='center' style='padding-bottom:20px;'>
<h2 style='margin:0;color:#111;'>Reset Your Password</h2>
</td>
</tr>

<tr>
<td style='color:#555;font-size:15px;line-height:1.6;padding-bottom:20px;'>
We received a request to reset your password.
Click the button below to create a new password.
</td>
</tr>

<tr>
<td align='center' style='padding:20px 0;'>
<a href='{resetLink}' 
style='background-color:#dc2626;color:#ffffff;
text-decoration:none;padding:14px 28px;
border-radius:6px;font-weight:bold;
display:inline-block;font-size:14px;'>
Reset Password
</a>
</td>
</tr>

<tr>
<td style='color:#777;font-size:13px;line-height:1.6;padding-top:10px;'>
This link will expire in <strong>1 hour</strong>.
If you did not request this request, please ignore this email.
</td>
</tr>

<tr>
<td style='padding-top:25px;font-size:12px;color:#999;word-break:break-all;'>
If the button doesn’t work, copy and paste this link:<br/>
<a href='{resetLink}' style='color:#dc2626;'>{resetLink}</a>
</td>
</tr>

<tr>
<td align='center' style='padding-top:30px;font-size:12px;color:#aaa;'>
© {DateTime.UtcNow.Year} Zamindar. All rights reserved.
</td>
</tr>

</table>

</td>
</tr>
</table>

</body>
</html>";

            await _email.SendAsync(
                user.Email,
                "Reset Your Password",
                htmlBody);
        }
        public async Task ResetPasswordAsync(
          string token,
          string newPassword)
        {
            var decodedToken = Uri.UnescapeDataString(token);

            var user = await _users.GetByResetTokenAsync(decodedToken);

            if (user == null ||
                user.PasswordResetTokenExpiresAt <= DateTime.UtcNow)
            {
                throw new Exception("Invalid or expired token.");
            }

            user.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(newPassword);

            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiresAt = null;

            user.RefreshTokens?.ForEach(t =>
            {
                t.IsRevoked = true;
                t.RevokedReason = "Password reset";
            });

            await _users.UpdateAsync(user);
        }

        private string GeneratePasswordResetToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);

            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
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

        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = await _users.GetAllUsersAsync();

            return users.Select(user => new User
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            }).ToList();
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            var user = await _users.GetByIdAsync(userId);

            if (user == null)
                throw new Exception("User not found");

            await _users.DeleteUserWithPropertiesAsync(userId);

            return true;
        }

    }

}
