using BlazorApp3.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorApp3.Components.UI;

/// <summary>
/// Base dos editores em dialog (Drawer/Modal): carrega o modelo ao abrir, controla o "salvando"
/// e fecha + notifica o pai após salvar. Elimina o ciclo abrir/carregar/salvar repetido em cada editor.
/// </summary>
public abstract class EditorBase<TModel> : ComponentBase where TModel : class
{
    [Inject] protected UiDispatcher Ui { get; set; } = default!;
    [Inject] protected ToastService Toast { get; set; } = default!;

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }

    protected TModel? Model { get; private set; }
    protected bool Saving { get; private set; }
    private bool _wasVisible;

    /// <summary>Retorna o modelo para edição (ou um novo); null = não encontrado.</summary>
    protected abstract Task<TModel?> LoadModelAsync();

    /// <summary>Envia o command de salvar; retorna true em caso de sucesso.</summary>
    protected abstract Task<bool> SaveAsync(TModel model);

    protected override async Task OnParametersSetAsync()
    {
        if (Visible && !_wasVisible)
        {
            _wasVisible = true;
            Model = null;
            Model = await LoadModelAsync();
            if (Model is null)
            {
                Toast.Error("Registro não encontrado");
                await CloseAsync();
            }
        }
        else if (!Visible)
        {
            _wasVisible = false;
        }
    }

    protected async Task SubmitAsync()
    {
        if (Model is null || Saving) return;
        Saving = true;
        try
        {
            if (await SaveAsync(Model))
            {
                await CloseAsync();
                await OnSaved.InvokeAsync();
            }
        }
        finally
        {
            Saving = false;
        }
    }

    protected Task CloseAsync() => VisibleChanged.InvokeAsync(false);
}
