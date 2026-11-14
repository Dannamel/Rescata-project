using Microsoft.EntityFrameworkCore;

namespace Pickups.Api.Core.Pagination;

public static class QueryableExtensions
{

    public static async Task<PaginationResponse<T>> ToPagedListAsync<T>(
        this IQueryable<T> queryable,
        PaginationRequest request,
        CancellationToken cancellationToken = default)
    {
        int totalCount = await queryable.CountAsync(cancellationToken);

        List<T> items = await queryable
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginationResponse<T>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
