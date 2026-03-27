using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
 
namespace POS.Application.Features.Discount
{
    public record DiscountDeleteCommand(int Id) : IRequest<ApiResponse<bool>>;
 
    public class DiscountDeleteCommandHandler : IRequestHandler<DiscountDeleteCommand, ApiResponse<bool>>
    {
        private readonly IMyAppDbContext _context;
 
        public DiscountDeleteCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }
 
        public async Task<ApiResponse<bool>> Handle(DiscountDeleteCommand request, CancellationToken cancellationToken)
        {
            var discount = await _context.Discounts
                .FirstOrDefaultAsync(d => d.Id == request.Id && !d.IsDeleted, cancellationToken);
 
            if (discount == null)
                return ApiResponse<bool>.NotFound($"Discount with id {request.Id} not found.");
 
            discount.IsDeleted   = true;
            discount.DeletedDate = DateTimeOffset.UtcNow;
 
            await _context.SaveChangesAsync(cancellationToken);
 
            return ApiResponse<bool>.Ok(true, "Discount deleted successfully");
        }
    }
}