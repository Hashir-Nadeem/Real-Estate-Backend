namespace Real_Estate_WebAPI.Models
{
    public class RefreshToken
    {
        public string Token { get; set; }

        public DateTime ExpiresAt { get; set; }

        public bool IsRevoked { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 🔥 NEW (VERY IMPORTANT)
        public string? ReplacedByToken { get; set; }

        public string? RevokedReason { get; set; }

        public string? Device { get; set; }
        public string? IpAddress { get; set; }


    }

}
