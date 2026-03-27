using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
 
namespace POS.Application.Features.Category
{
    public record GetCategoryQuery : IRequest<ApiResponse<CategoryInfo>>
    {
        public int Id { get; set; }
    }
 
    public class GetCategoryQueryHandler : IRequestHandler<GetCategoryQuery, ApiResponse<CategoryInfo>>
    {
        private readonly IMyAppDbContext _context;
 
        public GetCategoryQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }
 
        public async Task<ApiResponse<CategoryInfo>> Handle(GetCategoryQuery request, CancellationToken cancellationToken)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, cancellationToken);
 
            if (category == null)
                return ApiResponse<CategoryInfo>.NotFound($"Category with id {request.Id} not found");
 
            return ApiResponse<CategoryInfo>.Ok(category.Adapt<CategoryInfo>());
        }
    }
}