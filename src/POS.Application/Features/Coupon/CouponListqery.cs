// POS.Application/Features/Coupon/CouponListqery.cs

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Coupon
{
    public class CouponListQuery : PaginationRequest, IRequest<PaginatedResult<CouponInfo>>
    {
        public string? Search { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
    }

    public class CouponListQueryValidator : AbstractValidator<CouponListQuery>
    {
        public CouponListQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    public class CouponListQueryHandler : IRequestHandler<CouponListQuery, PaginatedResult<CouponInfo>>
    {
        private readonly IMyAppDbContext _context;

        public CouponListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<CouponInfo>> Handle(CouponListQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Coupons.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(c => c.Name.Contains(request.Search) || c.Code.Contains(request.Search));
            }

            // FIX: Use .HasValue and .Value to safely access nullable dates
            if (request.StartDate.HasValue)
            {
                query = query.Where(c => c.StartDate >= request.StartDate.Value);
            }
            query = query.OrderByDescending(c => c.CreatedDate);

            // Project to CouponInfo
            var projectedQuery = query.Select(c => new CouponInfo
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                ConditionValue = c.ConditionValue,
                Limit = c.Limit,
                Discount = c.Discount,
                StartDate = c.StartDate,
                ExpiryDate = c.ExpiryDate,
                OncePerCustomer = c.OncePerCustomer,
                Description = c.Description,
                IsDeleted = c.IsDeleted,
                CreatedDate = c.CreatedDate,
                UpdatedDate = c.UpdatedDate,
            });

            return await projectedQuery.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}