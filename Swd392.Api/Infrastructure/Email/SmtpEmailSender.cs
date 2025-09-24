using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Swd392.Api.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _opts;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _opts = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opts.Host))
            throw new InvalidOperationException("SMTP Host is not configured (Smtp:Host)");

        using var client = new SmtpClient(_opts.Host, _opts.Port)
        {
            EnableSsl = _opts.UseSsl,
            Credentials = new NetworkCredential(_opts.Username, _opts.Password)
        };

        if (_opts.UseStartTls)
        {
            client.EnableSsl = true; 
        }

        using var msg = new MailMessage
        {
            From = new MailAddress(string.IsNullOrWhiteSpace(_opts.From) ? _opts.Username : _opts.From),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        msg.To.Add(new MailAddress(to));

        try
        {
            await client.SendMailAsync(msg, ct);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP send failed");
            throw;
        }
    }
}
