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

        // TEMPORARY DIAGNOSTICS - remove after debugging 535 auth issue
        if (_config is IConfigurationRoot diagRoot)
        {
            Console.WriteLine("[BrevoDiag] IConfiguration runtime type: " + _config.GetType().FullName);
            Console.WriteLine("[BrevoDiag] Providers (in load order):");
            foreach (var p in diagRoot.Providers)
            {
                Console.WriteLine("[BrevoDiag]   - " + p.ToString());
            }
        }
        Console.WriteLine($"[BrevoDiag] Host={host}");
        Console.WriteLine($"[BrevoDiag] Port={port}");
        Console.WriteLine($"[BrevoDiag] Username={username} UsernameHasLeadingOrTrailingWhitespace={username != username?.Trim()}");
        Console.WriteLine($"[BrevoDiag] FromEmail={fromEmail}");
        Console.WriteLine($"[BrevoDiag] FromName={fromName}");
        Console.WriteLine($"[BrevoDiag] PasswordIsNullOrEmpty={string.IsNullOrEmpty(password)}");
        Console.WriteLine($"[BrevoDiag] PasswordEqualsPlaceholder={password == "USE_ENV_VARIABLE"}");
        Console.WriteLine($"[BrevoDiag] PasswordLength={password?.Length ?? 0}");
        Console.WriteLine($"[BrevoDiag] PasswordStartsWithExpectedPrefix={password?.StartsWith("xsmtpsib-") ?? false}");
        Console.WriteLine($"[BrevoDiag] PasswordHasLeadingOrTrailingWhitespace={password != password?.Trim()}");
        // END TEMPORARY DIAGNOSTICS

        await client.AuthenticateAsync(username, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}