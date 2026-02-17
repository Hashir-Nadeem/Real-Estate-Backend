using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;
using Real_Estate_WebAPI.Settings;

namespace Real_Estate_WebAPI.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtp;

        public EmailService(IOptions<SmtpSettings> smtp)
        {
            _smtp = smtp.Value;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            var email = new MimeMessage();

            email.From.Add(MailboxAddress.Parse(_smtp.FromEmail));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;

            email.Body = new TextPart("html")
            {
                Text = body
            };

            using var smtpClient = new SmtpClient();

            await smtpClient.ConnectAsync(_smtp.Host, _smtp.Port, false);

            if (!string.IsNullOrEmpty(_smtp.Username))
            {
                await smtpClient.AuthenticateAsync(
                    _smtp.Username,
                    _smtp.Password);
            }

            await smtpClient.SendAsync(email);
            await smtpClient.DisconnectAsync(true);
        }
    }

}
