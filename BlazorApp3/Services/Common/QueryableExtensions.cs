using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Services;

public static class QueryableExtensions
{
    /// <summary>Pagina no banco (Count + Skip/Take), corrigindo página fora do intervalo.</summary>
    public static async Task<PagedResult<T>> ToPagedAsync<T>(this IQueryable<T> query, int page, int pageSize, CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        var total = await query.CountAsync(ct);
        var maxPage = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, maxPage);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<T>(items, total, page, pageSize);
    }

    /// <summary>Padrão "%termo%" para LIKE, escapando os curingas (use com escape "\\").</summary>
    public static string LikePattern(this string term) =>
        "%" + term.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
}
