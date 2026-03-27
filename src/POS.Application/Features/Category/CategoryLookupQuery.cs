// POS.Application/Features/Category/CategoryLookupQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Category
{

    // Query Request
    public class CategoryLookupListQuery : PaginationRequest, IRequest<PaginatedResult<CategoryInfoLookup>>
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
    }

    // Query Handler
    public class CategoryLookupListQueryHandler : IRequestHandler<CategoryLookupListQuery, PaginatedResult<CategoryInfoLookup>>
    {
        private readonly IMyAppDbContext _context;

        public CategoryLookupListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<CategoryInfoLookup>> Handle(CategoryLookupListQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Categories
                .Where(c => !c.IsDeleted)
                .AsNoTracking();

            // Search filter
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(c =>
                    c.Name.Contains(request.Search) ||
                    (c.Description != null && c.Description.Contains(request.Search))
                );
            }

            // Active status filter
            if (request.IsActive.HasValue)
            {
                query = query.Where(c => c.IsActive == request.IsActive.Value);
            }
            query = query.OrderByDescending(c => c.Id);

            // Project to CategoryInfo
            var projectedQuery = query.Select(c => new CategoryInfoLookup
            {
                Id = c.Id,
                Name = c.Name,
            });

            return await projectedQuery.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}