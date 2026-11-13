namespace BlazorApp3.Security;

/// <summary>
/// Eventos de sessão dentro do circuito (scoped): o SessionAlertHost recebe o "newSession" do SignalR e repassa
/// para quem estiver ouvindo (ex.: a tela de segurança recarrega a lista de sessões).
/// </summary>
public sealed class SessionEvents
{
    public event Action? Changed;
    public void RaiseChanged() => Changed?.Invoke();
}
