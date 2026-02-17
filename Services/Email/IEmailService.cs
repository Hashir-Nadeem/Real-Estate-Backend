namespace Real_Estate_WebAPI.Services.Email
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);
    }

}
