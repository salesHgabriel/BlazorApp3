namespace BlazorApp3.Localization;

/// <summary>
/// Mensagens de validação traduzidas. DataAnnotations exige propriedades estáticas
/// (<c>ErrorMessageResourceType</c> + <c>ErrorMessageResourceName</c>); cada uma lê o resource file da cultura atual.
/// </summary>
public static class ValidationMessages
{
    public static string NameRequired => AppText.Get("Informe o nome");
    public static string NameLength3To150 => AppText.Get("O nome deve ter entre 3 e 150 caracteres");
    public static string EmailRequired => AppText.Get("Informe o e-mail");
    public static string EmailInvalid => AppText.Get("E-mail inválido");
    public static string PasswordRequired => AppText.Get("Informe a senha");
    public static string PasswordMinLength => AppText.Get("A senha deve ter no mínimo 6 caracteres");
    public static string RoleNameRequired => AppText.Get("Informe o nome do papel");
    public static string NameLength3To60 => AppText.Get("O nome deve ter entre 3 e 60 caracteres");
    public static string TeamNameRequired => AppText.Get("Informe o nome do time");
    public static string NameLength2To100 => AppText.Get("O nome deve ter entre 2 e 100 caracteres");
    public static string SlugFormat => AppText.Get("Use apenas letras minúsculas, números e hífens");
    public static string ColorRequired => AppText.Get("Informe a cor");
    public static string ColorInvalid => AppText.Get("Cor inválida");
}
