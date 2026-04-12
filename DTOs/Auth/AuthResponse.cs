namespace Real_Estate_WebAPI.DTOs.Auth
{
    public class AuthResponse
    {
        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }
        public object User { get; set; } // 👈 ADD THIS

        public DateTime AccessTokenExpiresAt { get; set; }
    }

}
