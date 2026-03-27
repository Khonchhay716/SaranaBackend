using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
 
namespace POS.Application.Features.Category
{
    public class CategoryListQuery : PaginationRequest, IRequest<PaginatedResult<CategoryInfo>>
    {
        public string? Search   { get; set; }
        public bool?   IsActive { get; set; }
    }
 
    public class CategoryListQueryHandler : IRequestHandler<CategoryListQuery, PaginatedResult<CategoryInfo>>
    {
        private readonly IMyAppDbContext _context;
 
        public CategoryListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }
 
        public async Task<PaginatedResult<CategoryInfo>> Handle(CategoryListQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Categories
                .Where(c => !c.IsDeleted)
                .AsNoTracking();
 
            if (!string.IsNullOrWhiteSpace(request.Search))
                query = query.Where(c =>
                    c.Name.Contains(request.Search) ||
                    (c.Description != null && c.Description.Contains(request.Search)));
 
            if (request.IsActive.HasValue)
                query = query.Where(c => c.IsActive == request.IsActive.Value);
 
            query = query.OrderBy(c => c.Name);
 
            var projectedQuery = query.Select(c => new CategoryInfo
            {
                Id          = c.Id,
                Name        = c.Name,
                Description = c.Description,
                Image       = c.Image,
                IsActive    = c.IsActive,
            });
 
            return await projectedQuery.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}