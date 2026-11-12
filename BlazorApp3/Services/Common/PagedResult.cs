namespace BlazorApp3.Services;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasMore => Page < TotalPages;
    public static PagedResult<T> Empty(int pageSize = 10) => new([], 0, 1, pageSize);
}
