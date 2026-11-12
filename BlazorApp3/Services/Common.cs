using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Services;

/// <summary>Erro de regra de negócio, exibido ao usuário.</summary>
public sealed class AppException(string message) : Exception(message)
{
    public static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new AppException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasMore => Page < TotalPages;
    public static PagedResult<T> Empty(int pageSize = 10) => new([], 0, 1, pageSize);
}

public sealed record SelectOption<T>(T Value, string Label, string? Color = null);

public sealed record Badge(string Label, string Color);

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedAsync<T>(this IQueryable<T> query, int page, int pageSize, CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        var total = await query.CountAsync(ct);
        var maxPage = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, maxPage);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<T>(items, total, page, pageSize);
    }

    public static string LikePattern(this string term) =>
        "%" + term.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
}
