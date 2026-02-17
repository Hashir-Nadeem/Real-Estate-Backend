namespace Real_Estate_WebAPI.DTOs.Auth
{
    public class LoginRequest
    {
        public string EmailOrPhone { get; set; }

        public string Password { get; set; }
        public bool RememberMe { get; set; }

    }

}
