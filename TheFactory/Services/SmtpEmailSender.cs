using System.Net;
using System.Net.Mail;

namespace TheFactory.Services;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendAsync(
        string recipient,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var host = _configuration["Smtp:Host"];
        var senderAddress = _configuration["Smtp:From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(senderAddress))
        {
            throw new InvalidOperationException("SMTP host and sender address must be configured.");
        }

        var port = _configuration.GetValue("Smtp:Port", 587);
        if (port is < 1 or > 65535)
        {
            throw new InvalidOperationException("The configured SMTP port is invalid.");
        }

        using var message = new MailMessage(senderAddress, recipient)
        {
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = _configuration.GetValue("Smtp:EnableSsl", true)
        };

        var username = _configuration["Smtp:Username"];
        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, _configuration["Smtp:Password"]);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
