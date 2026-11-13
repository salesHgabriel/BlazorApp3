using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorApp3.Components.UI;

/// <summary>
/// Fecha dropdowns ao clicar fora sem usar o "dropdown-scrim" (camada fixed na tela toda).
/// O scrim engolia a rolagem do mouse: dentro de Drawer/Modal não dava para rolar até as opções.
/// Aqui o clique fora é detectado no documento (JS) e a rolagem continua livre.
/// </summary>
public sealed class OutsideClickWatcher(IJSRuntime js, Func<Task> onOutside) : IAsyncDisposable
{
    private DotNetObjectReference<OutsideClickWatcher>? _ref;
    private int? _id;

    public bool Active => _id is not null;

    /// <param name="reveal">Rola o painel para ficar visível (ex.: dropdown aberto no fim de um Drawer).</param>
    public async Task StartAsync(ElementReference element, bool reveal = true)
    {
        if (Active) return;
        _ref ??= DotNetObjectReference.Create(this);
        try { _id = await js.InvokeAsync<int>("appDropdown.watch", element, _ref, reveal); }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
    }

    public async Task StopAsync()
    {
        if (_id is not int id) return;
        _id = null;
        try { await js.InvokeVoidAsync("appDropdown.unwatch", id); }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
    }

    /// <summary>Sincroniza com o estado aberto/fechado do componente (chamar no OnAfterRenderAsync).</summary>
    public Task SyncAsync(bool open, ElementReference element) =>
        open ? StartAsync(element) : StopAsync();

    [JSInvokable]
    public Task OnOutside() => onOutside();

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _ref?.Dispose();
    }
}
