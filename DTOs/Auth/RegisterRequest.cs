namespace Real_Estate_WebAPI.DTOs.Auth
{
    public class RegisterRequest
    {
        public string FullName { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string Password { get; set; }
        public string Role { get; set; }


        public bool RememberMe { get; set; } = false;

    }

}
