using System.Net;
using BlazorApp3.Localization;
using Microsoft.AspNetCore.Components;

namespace BlazorApp3.Services;

/// <summary>Abre o dialog de confirmação (ConfirmHost) e aguarda a resposta do usuário.</summary>
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
        Title = AppText.Get("Confirmar exclusão"),
        Message = new MarkupString(AppText.Format("Tem certeza que deseja excluir <strong>{0}</strong>? Esta ação não pode ser desfeita.", WebUtility.HtmlEncode(itemName))),
        Details = details,
        ConfirmText = AppText.Get("Excluir"),
        Icon = "bi-trash3",
        Danger = true,
        RequireText = requireText
    });
}
