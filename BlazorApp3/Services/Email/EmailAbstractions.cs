namespace BlazorApp3.Services.Email;

/// <summary>Mensagem pronta para envio (o remetente é preenchido pelo <see cref="IMailSender"/> a partir da configuração).</summary>
public sealed record EmailMessage(string To, string? ToName, string Subject, string HtmlBody)
{
    public string From { get; init; } = "";
    public string? FromName { get; init; }
    /// <summary>Versão texto puro (opcional; os provedores geram a partir do HTML se vazio).</summary>
    public string? TextBody { get; init; }
}

/// <summary>
/// Provedor de envio de e-mail. Para suportar um provedor novo (Amazon SES, Postmark, Mailgun, Resend...):
/// implemente esta interface, registre com <c>services.AddEmailProvider&lt;MeuProvider&gt;()</c> e
/// selecione pelo <see cref="Name"/> em <c>Email:Provider</c> no appsettings.
/// </summary>
public interface IEmailProvider
{
    /// <summary>Nome usado em <c>Email:Provider</c> (comparação sem diferenciar maiúsculas).</summary>
    string Name { get; }

    /// <summary>Envia ou lança exceção (o Hangfire faz as novas tentativas).</summary>
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}

/// <summary>Ponto único de envio usado pela aplicação: aplica o remetente padrão e delega ao provedor configurado.</summary>
public interface IMailSender
{
    /// <summary>Nome do provedor ativo.</summary>
    string ProviderName { get; }

    Task SendAsync(string to, string? toName, string subject, string htmlBody, string? fromName = null, CancellationToken ct = default);
}
