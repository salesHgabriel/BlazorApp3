using System.ComponentModel.DataAnnotations;

namespace BlazorApp3.Data;

/// <summary>
/// Sessão de login (uma por navegador/dispositivo). O Id vai no claim "session_id" do cookie;
/// sessão revogada = cookie recusado na próxima requisição e circuito/SignalR derrubados na hora.
/// </summary>
public class UserSession
{
    [MaxLength(64)] public string Id { get; set; } = "";
    [MaxLength(450)] public string UserId { get; set; } = "";
    public AppUser? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public bool Persistent { get; set; }
    /// <summary>Login concluído com segundo fator (MFA).</summary>
    public bool Mfa { get; set; }

    // ---- Dispositivo (extraído do User-Agent no login)
    [MaxLength(64)] public string? IpAddress { get; set; }
    [MaxLength(512)] public string? UserAgent { get; set; }
    [MaxLength(60)] public string Browser { get; set; } = "";
    [MaxLength(60)] public string Os { get; set; } = "";
    /// <summary>desktop | mobile | tablet</summary>
    [MaxLength(20)] public string DeviceType { get; set; } = "desktop";

    // ---- Encerramento
    public DateTime? RevokedAt { get; set; }
    /// <summary>Ver <see cref="SessionRevokeReason"/>.</summary>
    [MaxLength(30)] public string? RevokedReason { get; set; }
    /// <summary>Sessão que encerrou esta (quando o próprio usuário encerrou de outro dispositivo).</summary>
    [MaxLength(64)] public string? RevokedBySessionId { get; set; }

    public ICollection<UserConnection> Connections { get; set; } = [];

    public bool IsActive => RevokedAt is null;
}

/// <summary>Conexão SignalR aberta por uma sessão (indica "online agora"). Limpa ao desconectar e na inicialização.</summary>
public class UserConnection
{
    [MaxLength(128)] public string ConnectionId { get; set; } = "";
    [MaxLength(64)] public string SessionId { get; set; } = "";
    public UserSession? Session { get; set; }
    [MaxLength(450)] public string UserId { get; set; } = "";
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
}

public static class SessionRevokeReason
{
    /// <summary>Saiu pelo menu.</summary>
    public const string Logout = "logout";
    /// <summary>Sessão única: entrou em outro lugar.</summary>
    public const string Replaced = "replaced";
    /// <summary>Encerrada pelo próprio usuário a partir de outra sessão.</summary>
    public const string Revoked = "revoked";
    /// <summary>Conta bloqueada/inativada por um administrador.</summary>
    public const string Blocked = "blocked";
    /// <summary>Mudança de segurança (senha, MFA).</summary>
    public const string Security = "security";
}
