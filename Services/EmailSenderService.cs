using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

public class EmailSenderService
{
    private readonly IConfiguration _config;

    public EmailSenderService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string to, string subject, string body, IFormFile? attachment)
    {
        var host = _config["BrevoSettings:Host"];
        var portText = _config["BrevoSettings:Port"];
        var username = _config["BrevoSettings:Username"];
        var password = _config["BrevoSettings:Password"];
        var fromEmail = _config["BrevoSettings:FromEmail"];
        var fromName = _config["BrevoSettings:FromName"];

        if (string.IsNullOrWhiteSpace(host))
            throw new Exception("Brevo Host is not loaded from appsettings.json");

        if (!int.TryParse(portText, out var port) || port <= 0)
            throw new Exception("Brevo Port is invalid in appsettings.json");

        if (string.IsNullOrWhiteSpace(username))
            throw new Exception("Brevo Username is missing in appsettings.json");

        if (string.IsNullOrWhiteSpace(password))
            throw new Exception("Brevo Password is missing in appsettings.json");

        if (string.IsNullOrWhiteSpace(fromEmail))
            throw new Exception("Brevo FromEmail is missing in appsettings.json");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName ?? string.Empty, fromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = body
        };

        if (attachment != null && attachment.Length > 0)
        {
            using var ms = new MemoryStream();
            await attachment.CopyToAsync(ms);

            var contentType = string.IsNullOrWhiteSpace(attachment.ContentType)
                ? "application/octet-stream"
                : attachment.ContentType;

            builder.Attachments.Add(
                attachment.FileName,
                ms.ToArray(),
                ContentType.Parse(contentType));
        }

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();

        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(username, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}