namespace ChatApplication.Infrastructure.External.Email;

public class EmailService
{
    public Task SendAsync(string to, string subject, string body)
    {
        // TODO: integrate SMTP or email provider
        return Task.CompletedTask;
    }
}
