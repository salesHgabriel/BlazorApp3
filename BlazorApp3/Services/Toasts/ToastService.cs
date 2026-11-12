namespace BlazorApp3.Services;

/// <summary>Fila de toasts do circuito (exibida pelo ToastHost).</summary>
public sealed class ToastService
{
    private readonly List<ToastMessage> _toasts = [];
    private readonly Lock _lock = new();

    public event Action? Changed;

    public IReadOnlyList<ToastMessage> Snapshot() { lock (_lock) return _toasts.ToList(); }

    public void Show(ToastLevel level, string title, string? message = null, int durationMs = 4500)
    {
        var toast = new ToastMessage { Level = level, Title = title, Message = message, DurationMs = durationMs };
        lock (_lock)
        {
            _toasts.Add(toast);
            if (_toasts.Count > 6) _toasts.RemoveAt(0);
        }
        Changed?.Invoke();
        if (durationMs > 0) _ = DismissLaterAsync(toast);
    }

    public void Success(string title, string? message = null) => Show(ToastLevel.Success, title, message);
    public void Error(string title, string? message = null) => Show(ToastLevel.Error, title, message, 7000);
    public void Warning(string title, string? message = null) => Show(ToastLevel.Warning, title, message, 6000);
    public void Info(string title, string? message = null) => Show(ToastLevel.Info, title, message);

    private async Task DismissLaterAsync(ToastMessage toast)
    {
        await Task.Delay(toast.DurationMs);
        await DismissAsync(toast.Id);
    }

    public async Task DismissAsync(Guid id)
    {
        ToastMessage? toast;
        lock (_lock) toast = _toasts.FirstOrDefault(t => t.Id == id);
        if (toast is null || toast.Leaving) return;
        toast.Leaving = true;
        Changed?.Invoke();
        await Task.Delay(280); // tempo da animação de saída
        lock (_lock) _toasts.Remove(toast);
        Changed?.Invoke();
    }
}
