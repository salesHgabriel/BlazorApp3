namespace BlazorApp3.Data;

/// <summary>Configurações do sistema que podem ser ligadas/desligadas pela tela (linha única, Id = 1).</summary>
public class SystemSettings
{
    public int Id { get; set; } = 1;

    // ---- Cadastro
    /// <summary>Exibe a tela pública "Criar conta".</summary>
    public bool RegistrationEnabled { get; set; } = true;
    /// <summary>Exige confirmar o e-mail por código antes do primeiro acesso.</summary>
    public bool RequireEmailConfirmation { get; set; } = true;
    /// <summary>Avisa quem tem a permissão Users.Edit quando alguém se cadastra.</summary>
    public bool NotifyAdminsOnRegistration { get; set; } = true;

    // ---- Segurança
    /// <summary>Sessão única: ao entrar em outro navegador/dispositivo, as demais sessões são encerradas.</summary>
    public bool SingleSessionEnabled { get; set; }
    /// <summary>Com sessão única desligada: avisa as sessões abertas (em tempo real) quando a conta entra em outro dispositivo.</summary>
    public bool NewLoginAlertsEnabled { get; set; } = true;
    /// <summary>Alertas de segurança (excesso de tentativas de senha) para o próprio usuário.</summary>
    public bool SecurityAlertsEnabled { get; set; } = true;

    // ---- Notificações
    /// <summary>Notificações no sistema (sino com badge, em tempo real via SignalR).</summary>
    public bool InAppNotificationsEnabled { get; set; } = true;
    /// <summary>Cópia das notificações por e-mail.</summary>
    public bool EmailNotificationsEnabled { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public SystemSettings Clone() => (SystemSettings)MemberwiseClone();
}
