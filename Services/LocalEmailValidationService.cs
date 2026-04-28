using System.Net.Mail;
using System.Net;

public class LocalEmailValidationService
{
    public bool IsValidFormat(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DomainExistsAsync(string email)
    {
        try
        {
            var domain = email.Split('@').Last();

            var hostEntry = await Dns.GetHostEntryAsync(domain);
            return hostEntry != null;
        }
        catch
        {
            return false;
        }
    }

    public bool IsDisposable(string email)
    {
        var disposableDomains = new List<string>
        {
            "mailinator.com",
            "tempmail.com",
            "10minutemail.com",
            "guerrillamail.com"
        };

        var domain = email.Split('@').Last().ToLower();

        return disposableDomains.Contains(domain);
    }
}