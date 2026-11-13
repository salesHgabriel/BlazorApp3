using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;

namespace BlazorApp3.Services.Email;

/// <summary>Qualquer servidor SMTP (Mailtrap Sandbox, Gmail, Office 365, Amazon SES SMTP, SendGrid SMTP...).</summary>
public sealed class SmtpEmailProvider(IOptions<EmailOptions> options) : IEmailProvider
{
    public string Name => "Smtp";

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var o = options.Value.Smtp;
        if (string.IsNullOrWhiteSpace(o.Host)) throw new InvalidOperationException("Email:Smtp:Host não configurado.");

        using var mail = MailMessageFactory.Create(message);
        using var client = new SmtpClient(o.Host, o.Port) { EnableSsl = o.EnableSsl };
        if (!string.IsNullOrEmpty(o.User)) client.Credentials = new NetworkCredential(o.User, o.Password);
        await client.SendMailAsync(mail, ct);
    }
}

/// <summary>Grava cada e-mail como arquivo .eml numa pasta (desenvolvimento: abra no Outlook/Thunderbird ou num editor).</summary>
public sealed class PickupDirectoryEmailProvider(IOptions<EmailOptions> options, IWebHostEnvironment env, ILogger<PickupDirectoryEmailProvider> logger) : IEmailProvider
{
    public string Name => "Pickup";

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var dir = options.Value.Pickup.Directory;
        if (!Path.IsPathRooted(dir)) dir = Path.Combine(env.ContentRootPath, dir);
        Directory.CreateDirectory(dir);

        using var mail = MailMessageFactory.Create(message);
        using var client = new SmtpClient { DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory, PickupDirectoryLocation = dir };
        await client.SendMailAsync(mail, ct);
        logger.LogInformation("E-mail \"{Subject}\" para {To} gravado em {Dir}.", message.Subject, message.To, dir);
    }
}

internal static class MailMessageFactory
{
    public static MailMessage Create(EmailMessage m)
    {
        var mail = new MailMessage
        {
            From = new MailAddress(m.From, m.FromName ?? m.From, Encoding.UTF8),
            Subject = m.Subject,
            SubjectEncoding = Encoding.UTF8,
            Body = m.HtmlBody,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true
        };
        mail.To.Add(new MailAddress(m.To, m.ToName ?? m.To, Encoding.UTF8));
        return mail;
    }
}
