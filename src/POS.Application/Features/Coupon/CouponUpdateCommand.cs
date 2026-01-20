using System.ComponentModel;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using POS.Application.Features.Coupon;
using POS.Domain.Enums.Coupon;

namespace POS.Application.Features.Coupon
{
    public record CouponUpdateCommand : IRequest<ApiResponse<CouponInfo>>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public ConditionType ConditionType { get; set; }
        public decimal ConditionValue { get; set; }
        public DiscountType Type { get; set; }
        public int? Limit { get; set; }
        public decimal Discount { get; set; }
        [DefaultValue(null)]
        public DateTime? StartDate { get; set; }
        [DefaultValue(null)]
        public DateTime? ExpiryDate { get; set; }
        public bool OncePerCustomer { get; set; }
        public string Description { get; set; } = string.Empty;
        public Status Status { get; set; }
    }

    public class CouponUpdateCommandValidator : AbstractValidator<CouponUpdateCommand>
    {
        public CouponUpdateCommandValidator()
        {
        }
    }

    public class CouponUpdateCommandHandler : IRequestHandler<CouponUpdateCommand, ApiResponse<CouponInfo>>
    {
        private readonly IMyAppDbContext _context;

        public CouponUpdateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<CouponInfo>> Handle(CouponUpdateCommand request, CancellationToken cancellationToken)
        {
            var validator = new CouponUpdateCommandValidator();
            var validationResult = validator.Validate(request);

            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return ApiResponse<CouponInfo>.BadRequest(errorMessages);
            }

            var Coupon = await _context.Coupons
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (Coupon == null)
            {
                return ApiResponse<CouponInfo>.NotFound($"Coupon with id {request.Id} was not found");
            }

            request.Adapt(Coupon);
            Coupon.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            var CouponInfo = Coupon.Adapt<CouponInfo>();
            CouponInfo.Status = new TypeNamebase
            {
                Id = (int)Coupon.Status,
                Name = Coupon.Status.ToString(),
            };
            CouponInfo.Type = new TypeNamebase
            {
                Id = (int)Coupon.Type,
                Name = Coupon.Type.ToString(),
            };
            CouponInfo.ConditionType = new TypeNamebase
            {
                Id = (int)Coupon.ConditionType,
                Name = Coupon.ConditionType.ToString(),
            };
            return ApiResponse<CouponInfo>.Ok(CouponInfo, $"Coupon with id {request.Id} was updated successfully");
        }
    }
}