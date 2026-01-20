using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;

namespace POS.Application.Common.Extensions
{
    public static class QueryableExtensions
    {
        public static async Task<PaginatedResult<T>> ToPaginatedResultAsync<T>(
            this IQueryable<T> query, 
            int page, 
            int pageSize,
            CancellationToken cancellationToken = default) where T : class
        {
            var totalCount = await query.CountAsync(cancellationToken);
            
            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginatedResult<T>
            {
                data = data,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}