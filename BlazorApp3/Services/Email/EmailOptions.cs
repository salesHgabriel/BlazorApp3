namespace BlazorApp3.Services.Email;

/// <summary>
/// Seção "Email" do appsettings. <see cref="Provider"/> escolhe o provedor: Pickup (arquivos .eml, padrão em desenvolvimento),
/// Smtp (qualquer servidor SMTP: Mailtrap Sandbox, Gmail, Office 365, SES SMTP...), SendGrid ou Mailtrap (API HTTP).
/// Segredos (senhas, API keys) devem ir para user-secrets ou variáveis de ambiente, ex.: <c>Email__SendGrid__ApiKey</c>.
/// </summary>
public sealed class EmailOptions
{
    public const string Section = "Email";

    public string Provider { get; set; } = "Pickup";
    public string FromAddress { get; set; } = "no-reply@nexus.dev";
    public string? FromName { get; set; }

    public PickupOptions Pickup { get; set; } = new();
    public SmtpOptions Smtp { get; set; } = new();
    public SendGridOptions SendGrid { get; set; } = new();
    public MailtrapOptions Mailtrap { get; set; } = new();

    /// <summary>
    /// Indica se há um provedor que entrega e-mails de verdade. Pickup (só grava .eml em disco), provedor vazio ou
    /// provedores embutidos sem credenciais contam como "não configurado". Provedores customizados são considerados configurados.
    /// </summary>
    public bool IsDeliveryConfigured() => Provider?.Trim().ToLowerInvariant() switch
    {
        null or "" or "pickup" => false,
        "smtp" => !string.IsNullOrWhiteSpace(Smtp.Host),
        "sendgrid" => !string.IsNullOrWhiteSpace(SendGrid.ApiKey),
        "mailtrap" => !string.IsNullOrWhiteSpace(Mailtrap.ApiToken),
        _ => true
    };

    public sealed class PickupOptions
    {
        public string Directory { get; set; } = "App_Data/emails";
    }

    public sealed class SmtpOptions
    {
        public string? Host { get; set; }
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string? User { get; set; }
        public string? Password { get; set; }
    }

    public sealed class SendGridOptions
    {
        public string? ApiKey { get; set; }
        public string Endpoint { get; set; } = "https://api.sendgrid.com/v3/mail/send";
    }

    public sealed class MailtrapOptions
    {
        public string? ApiToken { get; set; }
        /// <summary>Produção: https://send.api.mailtrap.io/api/send · Sandbox: https://sandbox.api.mailtrap.io/api/send/{inboxId}</summary>
        public string Endpoint { get; set; } = "https://send.api.mailtrap.io/api/send";
    }
}
