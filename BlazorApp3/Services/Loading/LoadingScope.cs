namespace BlazorApp3.Services;

/// <summary>Encerra o loading uma única vez, mesmo que Dispose seja chamado de novo.</summary>
internal sealed class LoadingScope(Action end) : IDisposable
{
    private int _disposed;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0) end();
    }
}
