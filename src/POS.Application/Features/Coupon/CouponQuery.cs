using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using POS.Application.Features.Coupon;

namespace POS.Application.Features.Person
{
    public class CouponQuery : IRequest<ApiResponse<CouponInfo>>
    {
        public int Id { get; set; }
    }
    public class CouponQueryHandler : IRequestHandler<CouponQuery, ApiResponse<CouponInfo>>
    {
        private readonly IMyAppDbContext _context;

        public CouponQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<CouponInfo>> Handle(CouponQuery request, CancellationToken cancellationToken)
        {
            var coupon = await _context.Coupons
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (coupon == null)
            {
                return ApiResponse<CouponInfo>.NotFound($"Coupon with id {request.Id} was not found");
            }
            var info = coupon.Adapt<CouponInfo>();
            info.Status = new TypeNamebase
            {
                Id = (int)coupon.Status,
                Name = coupon.Status.ToString(),
            };
            info.Type = new TypeNamebase
            {
                Id = (int)coupon.Type,
                Name = coupon.Type.ToString(),
            };
            info.ConditionType = new TypeNamebase
            {
                Id = (int)coupon.ConditionType,
                Name = coupon.ConditionType.ToString(),
            };

            return ApiResponse<CouponInfo>.Ok(info, "Person retrieved successfully");
        }
    }
}