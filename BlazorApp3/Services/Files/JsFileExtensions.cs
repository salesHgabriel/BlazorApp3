using Microsoft.JSInterop;

namespace BlazorApp3.Services;

/// <summary>Download (via stream, sem limite de mensagem do SignalR) e cópia para a área de transferência.</summary>
public static class JsFileExtensions
{
    public static async Task DownloadAsync(this IJSRuntime js, string fileName, string contentType, byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var streamRef = new DotNetStreamReference(stream);
        await js.InvokeVoidAsync("appFiles.download", fileName, contentType, streamRef);
    }

    public static ValueTask<bool> CopyToClipboardAsync(this IJSRuntime js, string text) =>
        js.InvokeAsync<bool>("appFiles.copy", text);
}
