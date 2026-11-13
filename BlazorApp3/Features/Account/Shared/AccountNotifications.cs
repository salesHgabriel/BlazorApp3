using BlazorApp3.Data;
using BlazorApp3.Features.Notifications;
using BlazorApp3.Localization;
using BlazorApp3.Security;

namespace BlazorApp3.Features.Account;

/// <summary>Notificações (sistema + e-mail) dos eventos de conta. Textos no idioma de quem disparou a ação.</summary>
public static class AccountNotifications
{
    public static void Welcome(this INotifier n, AppUser user) => n.Publish(new NotificationRequest
    {
        UserIds = [user.Id],
        Title = AppText.Get("Conta confirmada!"),
        Message = AppText.Get("Seu e-mail foi confirmado e sua conta já está pronta para uso."),
        Icon = "bi-patch-check", Level = NotificationLevel.Success, Link = ""
    });

    public static void NewRegistration(this INotifier n, AppUser user) => n.Publish(new NotificationRequest
    {
        Permission = Perm.UsersEdit,
        Title = AppText.Get("Novo cadastro"),
        Message = AppText.Format("{0} ({1}) criou uma conta.", user.FullName, user.Email),
        Icon = "bi-person-plus", Level = NotificationLevel.Info, Link = "users"
    });

    public static void Blocked(this INotifier n, AppUser user)
    {
        var until = BlockState.IsPermanent(user.LockoutEnd)
            ? AppText.Get("O bloqueio é por tempo indeterminado.")
            : AppText.Format("O bloqueio vale até {0} (UTC).", user.LockoutEnd!.Value.UtcDateTime.ToString("g"));
        var reason = string.IsNullOrWhiteSpace(user.BlockReason) ? "" : " " + AppText.Format("Motivo: {0}", user.BlockReason);
        n.Publish(new NotificationRequest
        {
            UserIds = [user.Id],
            Title = AppText.Get("Sua conta foi bloqueada"),
            Message = AppText.Get("Sua conta foi bloqueada por um administrador e o acesso está suspenso.") + " " + until + reason,
            Icon = "bi-lock", Level = NotificationLevel.Danger,
            InApp = false, // não consegue entrar para ver; vai só por e-mail
            IncludeInactive = true
        });
    }

    public static void Unblocked(this INotifier n, string userId) => n.Publish(new NotificationRequest
    {
        UserIds = [userId],
        Title = AppText.Get("Sua conta foi desbloqueada"),
        Message = AppText.Get("Seu acesso foi restabelecido. Você já pode entrar novamente."),
        Icon = "bi-unlock", Level = NotificationLevel.Success, Link = ""
    });

    public static void NewLogin(this INotifier n, UserSession s) => n.Publish(new NotificationRequest
    {
        UserIds = [s.UserId],
        Title = AppText.Get("Novo acesso à sua conta"),
        Message = AppText.Format("Entrada em {0} · {1} (IP {2}). Se não foi você, encerre a sessão e troque sua senha.", s.Browser, s.Os, s.IpAddress ?? "?"),
        Icon = "bi-shield-exclamation", Level = NotificationLevel.Warning, Link = "account/security",
        Security = true, Email = false
    });

    public static void LockoutAlert(this INotifier n, string userId) => n.Publish(new NotificationRequest
    {
        UserIds = [userId],
        Title = AppText.Get("Alerta de segurança: tentativas de acesso"),
        Message = AppText.Get("Houve várias tentativas de entrar na sua conta com a senha incorreta, por isso ela foi bloqueada temporariamente. Se não foi você, recomendamos trocar a senha."),
        Icon = "bi-shield-exclamation", Level = NotificationLevel.Warning, Security = true
    });
}
