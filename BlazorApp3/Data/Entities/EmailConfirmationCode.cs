using System.ComponentModel.DataAnnotations;

namespace BlazorApp3.Data;

/// <summary>
/// Código numérico de confirmação de e-mail. Só o hash é persistido; o código em claro
/// existe apenas no e-mail enviado (e cifrado nos argumentos do job do Hangfire).
/// </summary>
public class EmailConfirmationCode
{
    public int Id { get; set; }
    [MaxLength(450)] public string UserId { get; set; } = "";
    [MaxLength(64)] public string CodeHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; }
    /// <summary>Preenchido quando o código é usado ou substituído por um novo.</summary>
    public DateTime? ConsumedAt { get; set; }

    public AppUser User { get; set; } = null!;
}
