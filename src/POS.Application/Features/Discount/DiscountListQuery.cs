// POS.Application/Features/Discount/DiscountListQuery.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Discount
{
    public class DiscountListQuery : PaginationRequest, IRequest<PaginatedResult<DiscountInfo>>
    {
        public string? Search   { get; set; }
        public string? Type     { get; set; }  // "Percentage" | "FixedAmount"
        public bool?   IsActive { get; set; }
        public bool?   IsGlobal { get; set; }  // true = global | false = specific | null = all
    }

    public class DiscountListQueryValidator : AbstractValidator<DiscountListQuery>
    {
        public DiscountListQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    public class DiscountListQueryHandler : IRequestHandler<DiscountListQuery, PaginatedResult<DiscountInfo>>
    {
        private readonly IMyAppDbContext _context;

        public DiscountListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<DiscountInfo>> Handle(
            DiscountListQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Discounts
                .Where(d => !d.IsDeleted)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Search))
                query = query.Where(d =>
                    d.Name.Contains(request.Search) ||
                    (d.Description != null && d.Description.Contains(request.Search)));

            if (!string.IsNullOrWhiteSpace(request.Type))
                query = query.Where(d => d.Type == request.Type);

            if (request.IsActive.HasValue)
                query = query.Where(d => d.IsActive == request.IsActive.Value);

            if (request.IsGlobal.HasValue)
            {
                if (request.IsGlobal.Value)
                    query = query.Where(d => !d.ProductDiscounts.Any(pd => !pd.IsDeleted));
                else
                    query = query.Where(d => d.ProductDiscounts.Any(pd => !pd.IsDeleted));
            }

            query = query.OrderByDescending(d => d.CreatedDate);

            var projectedQuery = query.Select(d => new DiscountInfo
            {
                Id             = d.Id,
                Name           = d.Name,
                Description    = d.Description,
                Type           = d.Type,
                Value          = d.Value,
                MinOrderAmount = d.MinOrderAmount,
                StartDate      = d.StartDate,
                EndDate        = d.EndDate,
                IsActive       = d.IsActive,
                IsGlobal       = !d.ProductDiscounts.Any(pd => !pd.IsDeleted),
                IsDeleted      = d.IsDeleted,
                CreatedDate    = d.CreatedDate,
                CreatedBy      = d.CreatedBy,
                UpdatedDate    = d.UpdatedDate,
                UpdatedBy      = d.UpdatedBy,
                Products = d.ProductDiscounts
                    .Where(pd => !pd.IsDeleted)
                    .Select(pd => new DiscountProductItem
                    {
                        ProductDiscountId = pd.Id,
                        ProductId         = pd.ProductId,
                        ProductName       = pd.Product.Name,
                        ProductSKU        = pd.Product.SKU,
                        ImageProduct      = pd.Product.ImageProduct,
                        Price             = pd.Product.Price,
                    }).ToList(),
            });

            return await projectedQuery.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}