using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Services;

public readonly record struct CommandResult<T>(bool Succeeded, T? Value)
{
    public static CommandResult<T> Ok(T value) => new(true, value);
    public static readonly CommandResult<T> Failed = new(false, default);
}

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
        string errorTitle = "Não foi possível concluir")
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
            toast.Error(errorTitle, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao executar {Command}", command.GetType().Name);
            toast.Error(errorTitle, "Ocorreu um erro inesperado. Tente novamente.");
        }
        return CommandResult<TResult>.Failed;
    }

    /// <summary>Confirmação de exclusão padrão + execução com loading e toast.</summary>
    public async Task<bool> DeleteAsync<TResult>(ICommand<TResult> command, string itemName,
        string? details = null, string? requireText = null, string success = "Excluído com sucesso")
    {
        if (!await confirm.DeleteAsync(itemName, details, requireText)) return false;
        return (await SendAsync(command, success, itemName, "Excluindo...", "Não foi possível excluir")).Succeeded;
    }

    /// <summary>Confirmação customizada + execução.</summary>
    public async Task<bool> ConfirmAndSendAsync<TResult>(ConfirmOptions options, ICommand<TResult> command,
        string? success = null, string? successDetail = null, string? loadingText = null)
    {
        if (!await confirm.ShowAsync(options)) return false;
        return (await SendAsync(command, success, successDetail, loadingText)).Succeeded;
    }
}

/// <summary>Estado reutilizável de listagens paginadas (antes duplicado em cada página).</summary>
public sealed class PagedQueryState<TItem>(IDispatcher dispatcher, ToastService toast, PagedQuery<TItem> query, Action stateChanged)
{
    private int _version;

    public PagedQuery<TItem> Query => query;
    public PagedResult<TItem> Result { get; private set; } = PagedResult<TItem>.Empty(query.PageSize);
    public IReadOnlyList<TItem> Items => Result.Items;
    public bool Loading { get; private set; } = true;
    public bool IsInitialLoad => Loading && Result.Items.Count == 0;
    public bool IsRefreshing => Loading && Result.Items.Count > 0;
    public bool IsEmpty => !Loading && Result.Items.Count == 0;

    public async Task LoadAsync()
    {
        var version = ++_version;
        Loading = true;
        stateChanged();
        try
        {
            var result = await dispatcher.QueryAsync(query);
            if (version != _version) return; // resposta obsoleta
            Result = result;
            query.Page = result.Page;
        }
        catch (Exception ex)
        {
            toast.Error("Erro ao carregar dados", ex.Message);
        }
        finally
        {
            if (version == _version)
            {
                Loading = false;
                stateChanged();
            }
        }
    }

    /// <summary>Aplica uma mudança de filtro e volta para a página 1.</summary>
    public Task ApplyAsync(Action change)
    {
        change();
        query.Page = 1;
        return LoadAsync();
    }

    public Task GoToPageAsync(int page)
    {
        query.Page = page;
        return LoadAsync();
    }

    public Task SetPageSizeAsync(int size)
    {
        query.PageSize = size;
        query.Page = 1;
        return LoadAsync();
    }
}
