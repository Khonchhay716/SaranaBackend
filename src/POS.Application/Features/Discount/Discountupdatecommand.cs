// POS.Application/Features/Discount/DiscountUpdateCommand.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using DomainProductDiscount = POS.Domain.Entities.ProductDiscount;
using static POS.Application.Features.Discount.DiscountCreateCommandHandler;

namespace POS.Application.Features.Discount
{
    public record DiscountUpdateCommand : IRequest<ApiResponse<DiscountInfo>>
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public int Id { get; set; }

        public string  Name            { get; set; } = string.Empty;
        public string? Description     { get; set; }
        public string  Type            { get; set; } = "Percentage";
        public decimal Value           { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate   { get; set; }
        public bool    IsActive        { get; set; } = true;

        /// <summary>
        /// Send empty list → make Global (remove all product restrictions).
        /// Send product IDs → replace product list with these.
        /// </summary>
        public List<int> ProductIds    { get; set; } = new();
    }

    public class DiscountUpdateCommandValidator : AbstractValidator<DiscountUpdateCommand>
    {
        public DiscountUpdateCommandValidator()
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

    public class DiscountUpdateCommandHandler : IRequestHandler<DiscountUpdateCommand, ApiResponse<DiscountInfo>>
    {
        private readonly IMyAppDbContext _context;

        public DiscountUpdateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<DiscountInfo>> Handle(DiscountUpdateCommand request, CancellationToken cancellationToken)
        {
            var discount = await _context.Discounts
                .Include(d => d.ProductDiscounts.Where(pd => !pd.IsDeleted))
                .FirstOrDefaultAsync(d => d.Id == request.Id && !d.IsDeleted, cancellationToken);

            if (discount == null)
                return ApiResponse<DiscountInfo>.NotFound($"Discount with id {request.Id} not found.");

            // Validate product IDs
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

            // Update basic fields
            discount.Name           = request.Name;
            discount.Description    = request.Description;
            discount.Type           = request.Type;
            discount.Value          = request.Value;
            discount.MinOrderAmount = request.MinOrderAmount;
            discount.StartDate      = request.StartDate;
            discount.EndDate        = request.EndDate;
            discount.IsActive       = request.IsActive;
            discount.UpdatedDate    = DateTimeOffset.UtcNow;

            // ✅ Replace product list — soft delete all existing, add new ones
            foreach (var existing in discount.ProductDiscounts)
            {
                existing.IsDeleted   = true;
                existing.DeletedDate = DateTimeOffset.UtcNow;
            }

            foreach (var productId in request.ProductIds.Distinct())
            {
                discount.ProductDiscounts.Add(new DomainProductDiscount
                {
                    ProductId = productId,
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Reload
            var updated = await _context.Discounts
                .Include(d => d.ProductDiscounts.Where(pd => !pd.IsDeleted))
                    .ThenInclude(pd => pd.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

            return ApiResponse<DiscountInfo>.Ok(MapToInfo(updated!), "Discount updated successfully");
        }
    }
}