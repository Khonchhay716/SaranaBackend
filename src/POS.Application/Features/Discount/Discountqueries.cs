
// ═══════════════════════════════════════════════════════════════
// POS.Application/Features/Discount/DiscountQuery.cs
// ═══════════════════════════════════════════════════════════════
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using static POS.Application.Features.Discount.DiscountCreateCommandHandler;

namespace POS.Application.Features.Discount
{
    public class DiscountQuery : IRequest<ApiResponse<DiscountInfo>>
    {
        public int Id { get; set; }
    }

    public class DiscountQueryHandler : IRequestHandler<DiscountQuery, ApiResponse<DiscountInfo>>
    {
        private readonly IMyAppDbContext _context;

        public DiscountQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<DiscountInfo>> Handle(DiscountQuery request, CancellationToken cancellationToken)
        {
            var discount = await _context.Discounts
                .Include(d => d.ProductDiscounts.Where(pd => !pd.IsDeleted))
                    .ThenInclude(pd => pd.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == request.Id && !d.IsDeleted, cancellationToken);

            if (discount == null)
                return ApiResponse<DiscountInfo>.NotFound($"Discount with id {request.Id} not found.");

            return ApiResponse<DiscountInfo>.Ok(MapToInfo(discount), "Discount retrieved successfully");
        }
    }
}
