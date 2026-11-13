using Microsoft.Extensions.Options;

namespace BlazorApp3.Services.Email;

/// <summary>Seleciona o <see cref="IEmailProvider"/> configurado em <c>Email:Provider</c>.</summary>
public sealed class MailSender(IEnumerable<IEmailProvider> providers, IOptions<EmailOptions> options) : IMailSender
{
    public string ProviderName => Provider.Name;

    private IEmailProvider Provider
    {
        get
        {
            var name = options.Value.Provider;
            return providers.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException(
                    $"Provedor de e-mail '{name}' não registrado. Disponíveis: {string.Join(", ", providers.Select(p => p.Name))}.");
        }
    }

    public Task SendAsync(string to, string? toName, string subject, string htmlBody, string? fromName = null, CancellationToken ct = default)
    {
        var o = options.Value;
        var message = new EmailMessage(to, toName, subject, htmlBody)
        {
            From = o.FromAddress,
            FromName = fromName ?? o.FromName
        };
        return Provider.SendAsync(message, ct);
    }
}
