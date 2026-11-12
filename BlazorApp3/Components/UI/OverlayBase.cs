using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorApp3.Components.UI;

/// <summary>
/// Base para Modal (central) e Drawer (lateral): controla montagem, animação de entrada/saída,
/// fechar com ESC, clique no backdrop e foco.
/// </summary>
public abstract class OverlayBase : ComponentBase
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? Subtitle { get; set; }
    [Parameter] public string? Icon { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? Footer { get; set; }
    [Parameter] public RenderFragment? HeaderContent { get; set; }
    [Parameter] public bool CloseOnBackdrop { get; set; } = true;
    [Parameter] public bool CloseOnEsc { get; set; } = true;
    [Parameter] public bool ShowClose { get; set; } = true;
    [Parameter] public EventCallback OnClosed { get; set; }
    [Parameter] public string? Class { get; set; }

    protected bool Mounted { get; private set; }
    protected bool Shown { get; private set; }
    protected ElementReference Panel;
    private bool _focusPending;

    protected override async Task OnParametersSetAsync()
    {
        if (Visible && !Mounted)
        {
            Mounted = true;
            Shown = false;
            await Task.Delay(20); // permite que a transição CSS de entrada aconteça
            if (Visible)
            {
                Shown = true;
                _focusPending = true;
            }
        }
        else if (!Visible && Mounted)
        {
            Shown = false;
            await Task.Delay(250); // tempo da animação de saída
            if (!Visible) Mounted = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusPending && Shown)
        {
            _focusPending = false;
            try { await Panel.FocusAsync(preventScroll: true); } catch { /* elemento pode ter sido removido */ }
        }
    }

    public async Task CloseAsync()
    {
        if (!Visible) return;
        await VisibleChanged.InvokeAsync(false);
        await OnClosed.InvokeAsync();
    }

    protected Task OnBackdropClick() => CloseOnBackdrop ? CloseAsync() : Task.CompletedTask;

    protected Task OnKeyDown(KeyboardEventArgs e) =>
        CloseOnEsc && e.Key == "Escape" ? CloseAsync() : Task.CompletedTask;
}
