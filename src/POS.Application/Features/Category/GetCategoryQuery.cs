// POS.Application/Features/Category/GetCategoryQuery.cs
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, cancellationToken);

            if (category == null)
            {
                return ApiResponse<CategoryInfo>.NotFound($"Category with id {request.Id} not found");
            }

            // Use Adapt for mapping
            var result = category.Adapt<CategoryInfo>();
            result.BookCount = category.Books.Count(b => !b.IsDeleted);

            return ApiResponse<CategoryInfo>.Ok(result);
        }
    }
}