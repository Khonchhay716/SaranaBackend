using System.ComponentModel;
using FluentValidation;
using Mapster;
using MediatR;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using POS.Application.Features.Coupon;
using POS.Domain.Enums.Coupon;
using DomainPerson = POS.Domain.Entities.Coupon;

namespace POS.Application.Features.Coupon
{
    public record CouponCreateCommand : IRequest<ApiResponse<CouponInfo>>
    {
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
        // public List<IdBase> Products { get; set; }
        public string Description { get; set; } = string.Empty;
        public Status Status { get; set; }
    }

    public class CouponCreateCommandValidator : AbstractValidator<CouponCreateCommand>
    {
        public CouponCreateCommandValidator()
        {
        }
    }

    public class CouponCreateCommandHandler : IRequestHandler<CouponCreateCommand, ApiResponse<CouponInfo>>
    {
        private readonly IMyAppDbContext _context;

        public CouponCreateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<CouponInfo>> Handle(CouponCreateCommand request, CancellationToken cancellationToken)
        {
            var validator = new CouponCreateCommandValidator();
            var validationResult = validator.Validate(request);

            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return ApiResponse<CouponInfo>.BadRequest(errorMessages);
            }

            var coupon = request.Adapt<DomainPerson>();
            coupon.CreatedDate = DateTimeOffset.UtcNow;
            coupon.UpdatedDate = DateTimeOffset.UtcNow;

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync(cancellationToken);

            var data = coupon.Adapt<CouponInfo>();
            data.Status = new TypeNamebase
            {
                Id = (int)coupon.Status,
                Name = coupon.Status.ToString(),
            };
            data.Type = new TypeNamebase
            {
                Id = (int)coupon.Type,
                Name = coupon.Type.ToString(),
            };
            data.ConditionType = new TypeNamebase
            {
                Id = (int)coupon.ConditionType,
                Name = coupon.ConditionType.ToString(),
            };
            return ApiResponse<CouponInfo>.Created(data, "Coupon created successfully");
        }
    }
}
