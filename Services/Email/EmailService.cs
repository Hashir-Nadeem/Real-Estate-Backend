using Microsoft.Extensions.Options;
using Real_Estate_WebAPI.Services.Email;
using Real_Estate_WebAPI.Settings;
using Resend;


    public class EmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly ResendSettings _settings;

        public EmailService(
            IResend resend,
            IOptions<ResendSettings> settings)
        {
            _resend = resend;
            _settings = settings.Value;
        }

    public async Task SendAsync(string to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(to))
            return;

        to = to.Trim();

        var message = new EmailMessage
        {
            From = _settings.FromEmail,
            Subject = subject,
            HtmlBody = body
        };

        message.To.Add(to);

        try
        {
            await _resend.EmailSendAsync(message);
        }
        catch (Exception ex)
        {
            var key = _settings.ApiKey;

            throw new Exception(
                $"EMAIL_SEND_FAILED: {ex.Message}. " +
                $"KeyExists: {!string.IsNullOrWhiteSpace(key)}, " +
                $"KeyPrefix: {(key?.StartsWith("re_") == true ? "OK" : "INVALID")}");
        }
    }
}
