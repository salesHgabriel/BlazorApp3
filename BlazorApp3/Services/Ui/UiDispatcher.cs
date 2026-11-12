using BlazorApp3.Application.Cqrs;
using BlazorApp3.Localization;

namespace BlazorApp3.Services;

/// <summary>
/// Fachada do dispatcher para componentes: concentra o padrão repetido
/// "confirmar → loading → enviar command → toast de sucesso/erro".
/// </summary>
public sealed class UiDispatcher(IDispatcher dispatcher, ToastService toast, LoadingService loading, ConfirmService confirm, ILogger<UiDispatcher> logger)
{
    /// <summary>Query direta (erros sobem para o ErrorBoundary).</summary>
    public Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) => dispatcher.QueryAsync(query);

    /// <summary>Estado de lista paginada ligado a uma query (loading, paginação, filtros).</summary>
    public PagedQueryState<TItem> Paged<TItem>(PagedQuery<TItem> query, Action stateChanged) =>
        new(dispatcher, toast, query, stateChanged);

    /// <summary>Envia o command e trata feedback. Retorna falha (sem lançar) quando há erro.</summary>
    public async Task<CommandResult<TResult>> SendAsync<TResult>(ICommand<TResult> command,
        string? success = null, string? successDetail = null, string? loadingText = null,
        string? errorTitle = null)
    {
        try
        {
            using IDisposable? _ = loadingText is null ? null : loading.Begin(loadingText);
            var result = await dispatcher.SendAsync(command);
            if (success is not null) toast.Success(success, successDetail);
            return CommandResult<TResult>.Ok(result);
        }
        catch (AppException ex)
        {
            toast.Error(errorTitle ?? AppText.Get("Não foi possível concluir"), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao executar {Command}", command.GetType().Name);
            toast.Error(errorTitle ?? AppText.Get("Não foi possível concluir"), AppText.Get("Ocorreu um erro inesperado. Tente novamente."));
        }
        return CommandResult<TResult>.Failed;
    }

    /// <summary>Confirmação de exclusão padrão + execução com loading e toast.</summary>
    public async Task<bool> DeleteAsync<TResult>(ICommand<TResult> command, string itemName,
        string? details = null, string? requireText = null, string? success = null)
    {
        if (!await confirm.DeleteAsync(itemName, details, requireText)) return false;
        return (await SendAsync(command, success ?? AppText.Get("Excluído com sucesso"), itemName, AppText.Get("Excluindo..."), AppText.Get("Não foi possível excluir"))).Succeeded;
    }

    /// <summary>Confirmação customizada + execução.</summary>
    public async Task<bool> ConfirmAndSendAsync<TResult>(ConfirmOptions options, ICommand<TResult> command,
        string? success = null, string? successDetail = null, string? loadingText = null)
    {
        if (!await confirm.ShowAsync(options)) return false;
        return (await SendAsync(command, success, successDetail, loadingText)).Succeeded;
    }
}
