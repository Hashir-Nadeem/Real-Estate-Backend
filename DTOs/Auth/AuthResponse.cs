namespace Real_Estate_WebAPI.DTOs.Auth
{
    public class AuthResponse
    {
        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public DateTime AccessTokenExpiresAt { get; set; }
    }

}
