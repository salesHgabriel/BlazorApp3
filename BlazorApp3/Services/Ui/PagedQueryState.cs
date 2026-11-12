using BlazorApp3.Application.Cqrs;
using BlazorApp3.Localization;

namespace BlazorApp3.Services;

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
            toast.Error(AppText.Get("Erro ao carregar dados"), ex.Message);
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
