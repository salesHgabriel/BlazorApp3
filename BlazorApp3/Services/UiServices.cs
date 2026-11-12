using System.Net;
using Microsoft.AspNetCore.Components;

namespace BlazorApp3.Services;

// ---------------------------------------------------------------- Toasts
public enum ToastLevel { Success, Error, Warning, Info }

public sealed class ToastMessage
{
    public Guid Id { get; } = Guid.NewGuid();
    public ToastLevel Level { get; init; }
    public string Title { get; init; } = "";
    public string? Message { get; init; }
    public int DurationMs { get; init; }
    public bool Leaving { get; set; }
}

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

// ---------------------------------------------------------------- Loading global
public sealed class LoadingService
{
    private int _count;
    public bool IsLoading => _count > 0;
    public string? Message { get; private set; }
    public event Action? Changed;

    public IDisposable Begin(string? message = null)
    {
        Interlocked.Increment(ref _count);
        Message = message;
        Changed?.Invoke();
        return new Scope(this);
    }

    public async Task RunAsync(Func<Task> work, string? message = null)
    {
        using var _ = Begin(message);
        await work();
    }

    public async Task<T> RunAsync<T>(Func<Task<T>> work, string? message = null)
    {
        using var _ = Begin(message);
        return await work();
    }

    private void End()
    {
        if (Interlocked.Decrement(ref _count) <= 0)
        {
            _count = 0;
            Message = null;
        }
        Changed?.Invoke();
    }

    private sealed class Scope(LoadingService owner) : IDisposable
    {
        private int _disposed;
        public void Dispose() { if (Interlocked.Exchange(ref _disposed, 1) == 0) owner.End(); }
    }
}

// ---------------------------------------------------------------- Confirmação
public sealed class ConfirmOptions
{
    public string Title { get; set; } = "Confirmar ação";
    public MarkupString Message { get; set; }
    public string? Details { get; set; }
    public string ConfirmText { get; set; } = "Confirmar";
    public string CancelText { get; set; } = "Cancelar";
    public string Icon { get; set; } = "bi-question-circle";
    public bool Danger { get; set; }

    /// <summary>Se definido, o usuário precisa digitar este texto para habilitar a confirmação.</summary>
    public string? RequireText { get; set; }
}

public sealed class ConfirmService
{
    public event Func<ConfirmOptions, TaskCompletionSource<bool>, Task>? Requested;

    public async Task<bool> ShowAsync(ConfirmOptions options)
    {
        if (Requested is null) return false;
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await Requested.Invoke(options, tcs);
        return await tcs.Task;
    }

    public Task<bool> DeleteAsync(string itemName, string? details = null, string? requireText = null) => ShowAsync(new ConfirmOptions
    {
        Title = "Confirmar exclusão",
        Message = new MarkupString($"Tem certeza que deseja excluir <strong>{WebUtility.HtmlEncode(itemName)}</strong>? Esta ação não pode ser desfeita."),
        Details = details,
        ConfirmText = "Excluir",
        Icon = "bi-trash3",
        Danger = true,
        RequireText = requireText
    });
}
