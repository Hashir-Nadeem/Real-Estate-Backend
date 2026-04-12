
using Microsoft.AspNetCore.Mvc;
using Real_Estate_WebAPI.DTOs.Auth;
using Real_Estate_WebAPI.Services.Auth;


namespace Real_Estate_WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth)
        {
            _auth = auth;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            try
            {
                var response = await _auth.RegisterAsync(request);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            try
            {
                var response = await _auth.LoginAsync(request);

                return Ok(response);
            }
            catch (Exception)
            {
                // NEVER reveal why login failed
                return Unauthorized(new
                {
                    message = "Invalid credentials"
                });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
    ForgotPasswordRequest request)
        {
            await _auth.ForgotPasswordAsync(request.Email);

            // NEVER reveal if email exists
            return Ok(new
            {
                message = "If the email exists, a reset link was sent."
            });
        }


        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordRequest request)
        {
            try
            {
                await _auth.ResetPasswordAsync(
                    request.Token,
                    request.NewPassword);

                return Ok(new
                {
                    message = "Password reset successful."
                });
            }
            catch
            {
                return BadRequest(new
                {
                    message = "Invalid or expired token."
                });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(
    RefreshTokenRequest request)
        {
            try
            {
                var response = await _auth.RefreshTokenAsync(
                    request.RefreshToken);

                return Ok(response);
            }
            catch
            {
                return Unauthorized(new
                {
                    message = "Invalid refresh token"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _auth.GetAllUsersAsync();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            await _auth.DeleteUserAsync(id);
            return Ok(new { message = "User and properties deleted successfully" });
        }

    }

}
