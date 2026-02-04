// POS.Application/Features/Category/CategoryLookupQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Category
{
    // Lookup DTO - lightweight for dropdowns
    public class CategoryLookupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    // Query Request
    public class CategoryLookupQuery : IRequest<ApiResponse<List<CategoryLookupDto>>>
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
    }

    // Query Handler
    public class CategoryLookupQueryHandler : IRequestHandler<CategoryLookupQuery, ApiResponse<List<CategoryLookupDto>>>
    {
        private readonly IMyAppDbContext _context;

        public CategoryLookupQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<CategoryLookupDto>>> Handle(CategoryLookupQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Categories
                .Where(c => !c.IsDeleted)
                .AsNoTracking();

            // Filter by active status (default to active only)
            if (request.IsActive.HasValue)
            {
                query = query.Where(c => c.IsActive == request.IsActive.Value);
            }
            else
            {
                // By default, only show active categories in lookup
                query = query.Where(c => c.IsActive);
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(c =>
                    c.Name.Contains(request.Search) ||
                    (c.Description != null && c.Description.Contains(request.Search))
                );
            }

            // Order by name and take top 100 for performance
            var categories = await query
                .OrderBy(c => c.Name)
                .Take(100)
                .Select(c => new CategoryLookupDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    IsActive = c.IsActive
                })
                .ToListAsync(cancellationToken);

            return ApiResponse<List<CategoryLookupDto>>.Ok(categories);
        }
    }
}