namespace Real_Estate_WebAPI.DTOs.Auth
{
    public class ResetPasswordRequest
    {
        public string Token { get; set; }

        public string NewPassword { get; set; }
    }

}
