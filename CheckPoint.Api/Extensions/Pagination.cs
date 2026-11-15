using CheckPoint.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CheckPoint.Api.Extensions;

public static class Pagination
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const string InvalidPagingMessage = "Sayfa en az 1, sayfa boyutu 1 ile 100 arasında olmalıdır.";

    public static bool IsValid(int page, int pageSize) =>
        page >= 1 && pageSize >= 1 && pageSize <= MaxPageSize;

    public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> orderedQuery,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await orderedQuery.CountAsync(cancellationToken);
        var skip = (long)(page - 1) * pageSize;
        var items = await orderedQuery
            .Skip(skip > int.MaxValue ? int.MaxValue : (int)skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<T>(items, page, pageSize, totalCount);
    }
}
