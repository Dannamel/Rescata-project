using Donations.Application.Utilities.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Donations.Persistence.Extensions
{
    internal static class QueryableExtensions
    {
        public static async Task<PaginationResponse<T>> ToPagedListAsync<T>(this IQueryable<T> queryable,
                                                                            PaginationRequest request,
                                                                            CancellationToken cancellationToken) 
        {
            int totalCount = await queryable.CountAsync(cancellationToken);

            List<T> items = await queryable.Skip((request.PageNumber - 1) * request.PageSize)
                                           .Take(request.PageSize)
                                           .ToListAsync(cancellationToken);

            return new PaginationResponse<T>(items, totalCount, request.PageNumber, request.PageSize);
        }
    }
}
