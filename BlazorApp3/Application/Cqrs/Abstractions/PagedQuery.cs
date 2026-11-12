using BlazorApp3.Services;

namespace BlazorApp3.Application.Cqrs;

/// <summary>Query paginada: Page/PageSize ficam na própria query (filtros mutáveis ligados à UI).</summary>
public abstract class PagedQuery<TItem> : IQuery<PagedResult<TItem>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
