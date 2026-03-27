// POS.Application/Features/Discount/DiscountCreateCommand.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using DomainDiscount        = POS.Domain.Entities.Discount;
using DomainProductDiscount = POS.Domain.Entities.ProductDiscount;

namespace POS.Application.Features.Discount
{
    public record DiscountCreateCommand : IRequest<ApiResponse<DiscountInfo>>
    {
        public string  Name            { get; set; } = string.Empty;
        public string? Description     { get; set; }

        /// <summary>"Percentage" | "FixedAmount"</summary>
        public string  Type            { get; set; } = "Percentage";
        public decimal Value           { get; set; }

        public decimal? MinOrderAmount { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate   { get; set; }
        public bool    IsActive        { get; set; } = true;

        /// <summary>
        /// Leave empty → Global (all products).
        /// Provide IDs → Specific products only.
        /// </summary>
        public List<int> ProductIds    { get; set; } = new();
    }

    public class DiscountCreateCommandValidator : AbstractValidator<DiscountCreateCommand>
    {
        public DiscountCreateCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Type)
                .Must(t => t == "Percentage" || t == "FixedAmount")
                .WithMessage("Type must be 'Percentage' or 'FixedAmount'.");
            RuleFor(x => x.Value).GreaterThan(0);
            RuleFor(x => x.Value)
                .LessThanOrEqualTo(100)
                .When(x => x.Type == "Percentage")
                .WithMessage("Percentage value cannot exceed 100.");
            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("EndDate must be after StartDate.");
        }
    }

    public class DiscountCreateCommandHandler : IRequestHandler<DiscountCreateCommand, ApiResponse<DiscountInfo>>
    {
        private readonly IMyAppDbContext _context;

        public DiscountCreateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<DiscountInfo>> Handle(DiscountCreateCommand request, CancellationToken cancellationToken)
        {
            // Validate all product IDs exist
            if (request.ProductIds.Any())
            {
                var validIds = await _context.Products
                    .Where(p => request.ProductIds.Contains(p.Id) && !p.IsDeleted)
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken);

                var invalidIds = request.ProductIds.Except(validIds).ToList();
                if (invalidIds.Any())
                    return ApiResponse<DiscountInfo>.BadRequest(
                        $"Products not found: {string.Join(", ", invalidIds)}");
            }

            var discount = new DomainDiscount
            {
                Name           = request.Name,
                Description    = request.Description,
                Type           = request.Type,
                Value          = request.Value,
                MinOrderAmount = request.MinOrderAmount,
                StartDate      = request.StartDate,
                EndDate        = request.EndDate,
                IsActive       = request.IsActive,
            };

            // ✅ Attach specific products (if any — empty = global)
            foreach (var productId in request.ProductIds.Distinct())
            {
                discount.ProductDiscounts.Add(new DomainProductDiscount
                {
                    ProductId = productId,
                });
            }

            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync(cancellationToken);

            // Reload with products
            var created = await LoadDiscountAsync(discount.Id, cancellationToken);
            return ApiResponse<DiscountInfo>.Created(MapToInfo(created!), "Discount created successfully");
        }

        private Task<DomainDiscount?> LoadDiscountAsync(int id, CancellationToken ct) =>
            _context.Discounts
                .Include(d => d.ProductDiscounts)
                    .ThenInclude(pd => pd.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id, ct);

        internal static DiscountInfo MapToInfo(DomainDiscount d) => new()
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
                    ProductName       = pd.Product?.Name ?? "",
                    ProductSKU        = pd.Product?.SKU,
                    ImageProduct      = pd.Product?.ImageProduct,
                    Price             = pd.Product?.Price ?? 0,
                }).ToList(),
        };
    }
}