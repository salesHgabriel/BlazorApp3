namespace BlazorApp3.Features.Account;

/// <summary>Configurações de cadastro e confirmação de e-mail (seção "Account" do appsettings).</summary>
public sealed class AccountOptions
{
    public const string Section = "Account";

    /// <summary>Papel atribuído a quem se cadastra pela tela pública (ignorado se não existir).</summary>
    public string? DefaultRole { get; set; } = "Visualizador";

    public int CodeLength { get; set; } = 6;
    public int CodeLifetimeMinutes { get; set; } = 15;
    public int MaxCodeAttempts { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 60;

    /// <summary>Contas que não confirmaram o e-mail neste prazo são removidas pelo job diário.</summary>
    public int UnconfirmedAccountRetentionDays { get; set; } = 7;
}
