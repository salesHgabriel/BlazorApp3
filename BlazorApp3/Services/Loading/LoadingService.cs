namespace BlazorApp3.Services;

/// <summary>Overlay de loading global, com contador (várias operações simultâneas).</summary>
public sealed class LoadingService
{
    private int _count;
    public bool IsLoading => _count > 0;
    public string? Message { get; private set; }
    public event Action? Changed;

    /// <summary>Mostra o loading até o retorno ser descartado (use com <c>using</c>).</summary>
    public IDisposable Begin(string? message = null)
    {
        Interlocked.Increment(ref _count);
        Message = message;
        Changed?.Invoke();
        return new LoadingScope(End);
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
}
