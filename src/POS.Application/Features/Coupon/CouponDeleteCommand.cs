using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Coupon
{
    public record CouponDeleteCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
    }
    public class CouponDeleteCommandHandler : IRequestHandler<CouponDeleteCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;

        public CouponDeleteCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse> Handle(CouponDeleteCommand request, CancellationToken cancellationToken)
        {
            var Coupon = await _context.Coupons
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (Coupon == null)
            {
                return ApiResponse.NotFound($"Coupon with id {request.Id} was not found");
            }

            Coupon.IsDeleted = true;
            Coupon.DeletedDate = DateTimeOffset.UtcNow;
            Coupon.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Coupon with id {request.Id} deleted successfully");
        }
    }
}