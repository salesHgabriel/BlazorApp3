using BlazorApp3.Localization;
using Microsoft.AspNetCore.Components;

namespace BlazorApp3.Services;

public sealed class ConfirmOptions
{
    public string Title { get; set; } = AppText.Get("Confirmar ação");
    public MarkupString Message { get; set; }
    public string? Details { get; set; }
    public string ConfirmText { get; set; } = AppText.Get("Confirmar");
    public string CancelText { get; set; } = AppText.Get("Cancelar");
    public string Icon { get; set; } = "bi-question-circle";
    public bool Danger { get; set; }

    /// <summary>Se definido, o usuário precisa digitar este texto para habilitar a confirmação.</summary>
    public string? RequireText { get; set; }
}
