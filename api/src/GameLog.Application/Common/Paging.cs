using Microsoft.EntityFrameworkCore;

namespace GameLog.Application.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public static class Paging
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;

    /// <summary>Normalises client-supplied paging values; the page size is always capped.</summary>
    public static (int Page, int PageSize) Clamp(int? page, int? pageSize, int defaultSize = DefaultPageSize)
    {
        var p = Math.Max(page ?? 1, 1);
        var s = Math.Clamp(pageSize ?? defaultSize, 1, MaxPageSize);
        return (p, s);
    }

    public static async Task<PagedResult<T>> ToPagedAsync<T>(
        this IQueryable<T> query, int? page, int? pageSize, CancellationToken ct = default)
    {
        var (p, s) = Clamp(page, pageSize);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((p - 1) * s).Take(s).ToListAsync(ct);
        return new PagedResult<T>(items, p, s, total);
    }
}
