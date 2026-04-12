using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;

namespace POS.Application.Features.Order
{
    public record OrderSummaryQuery : IRequest<ApiResponse<OrderPreviewResponse>>
    {
        public List<OrderItemPreviewDto> Items { get; set; } = new();
    }

    public class OrderItemPreviewDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class OrderPreviewResponse
    {
        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalPayable { get; set; }
        public List<NearDiscountHint> NearDiscountHints { get; set; } = new();
    }

    public class NearDiscountHint
    {
        public string Message { get; set; } = string.Empty;
        public decimal AmountNeeded { get; set; }
        public string DiscountName { get; set; } = string.Empty;
        public List<string> ApplicableProducts { get; set; } = new();
    }

    public class OrderSummaryHandler : IRequestHandler<OrderSummaryQuery, ApiResponse<OrderPreviewResponse>>
    {
        private readonly IMyAppDbContext _context;
        private const decimal NearThresholdPercent = 0.75m;

        public OrderSummaryHandler(IMyAppDbContext context) => _context = context;

        public async Task<ApiResponse<OrderPreviewResponse>> Handle(
            OrderSummaryQuery request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            decimal runningSubtotal = 0;
            decimal runningTax = 0;
            var itemCalcs = new List<InternalItemCalculation>();

            foreach (var item in request.Items)
            {
                var prod = await _context.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId && !p.IsDeleted, cancellationToken);

                if (prod == null) continue;

                decimal sub = item.Quantity * item.UnitPrice;
                decimal tax = sub * (prod.TaxRate / 100m);

                runningSubtotal += sub;
                runningTax += tax;
                itemCalcs.Add(new InternalItemCalculation
                {
                    ProductId = prod.Id,
                    TotalWithTax = sub + tax
                });
            }

            decimal totalBeforeDiscount = runningSubtotal + runningTax;

            var activeDiscounts = await _context.Discounts
                .Include(d => d.ProductDiscounts)
                .Where(d => !d.IsDeleted && d.IsActive
                      && (d.StartDate == null || d.StartDate <= now)
                      && (d.EndDate == null || d.EndDate >= now))
                .ToListAsync(cancellationToken);

            decimal totalAutoDiscount = 0;
            var nearDiscountHints = new List<NearDiscountHint>();

            foreach (var d in activeDiscounts)
            {
                var pIds = d.ProductDiscounts
                    .Where(pd => !pd.IsDeleted)
                    .Select(pd => pd.ProductId)
                    .ToList();

                bool isProductSpecific = pIds.Any();

                decimal eligibleTotal = isProductSpecific
                    ? itemCalcs
                        .Where(p => pIds.Contains(p.ProductId))
                        .Sum(p => p.TotalWithTax)
                    : totalBeforeDiscount;
                bool hasMinOrder = d.MinOrderAmount.HasValue && d.MinOrderAmount.Value > 0;
                bool qualifies = !hasMinOrder || totalBeforeDiscount >= d.MinOrderAmount!.Value;

                if (qualifies)
                {
                    if (eligibleTotal <= 0) continue;

                    totalAutoDiscount += d.Type == "Percentage"
                        ? eligibleTotal * (d.Value / 100m)
                        : d.Value;
                }
                else if (hasMinOrder)
                {
                    decimal minAmount = d.MinOrderAmount!.Value;
                    decimal nearFloor = minAmount * NearThresholdPercent;
                    bool isNearMiss = totalBeforeDiscount >= nearFloor;

                    if (!isNearMiss) continue;
                    if (isProductSpecific && eligibleTotal <= 0) continue;

                    decimal amountNeeded = Math.Round(minAmount - totalBeforeDiscount, 2);
                    string discountLabel = d.Type == "Percentage"
                        ? $"{d.Value:0.##}% OFF"
                        : $"${d.Value:0.00} OFF";

                    string message = string.Empty;
                    List<string> applicableProducts = new();

                    if (isProductSpecific)
                    {
                        applicableProducts = await _context.Products
                            .AsNoTracking()
                            .Where(p => pIds.Contains(p.Id) && !p.IsDeleted)
                            .Select(p => p.Name)
                            .ToListAsync(cancellationToken);

                        string productList = applicableProducts.Any()
                            ? string.Join(", ", applicableProducts)
                            : "selected products";
                        message = $"Add ${amountNeeded:0.00} more to your order total " +
                                  $"to unlock {discountLabel} on {productList}!";
                    }
                    else
                    {
                        message = $"Add ${amountNeeded:0.00} more to unlock " +
                                  $"{discountLabel} on your entire order!";
                    }

                    nearDiscountHints.Add(new NearDiscountHint
                    {
                        DiscountName = d.Name ?? discountLabel,
                        AmountNeeded = amountNeeded,
                        Message = message,
                        ApplicableProducts = applicableProducts
                    });
                }
            }

            var result = new OrderPreviewResponse
            {
                SubTotal = Math.Round(runningSubtotal, 2),
                TotalTax = Math.Round(runningTax, 2),
                TotalDiscount = Math.Round(totalAutoDiscount, 2),
                TotalPayable = Math.Round(totalBeforeDiscount - totalAutoDiscount, 2),
                NearDiscountHints = nearDiscountHints
            };

            return ApiResponse<OrderPreviewResponse>.Ok(result);
        }
    }

    internal class InternalItemCalculation
    {
        public int ProductId { get; set; }
        public decimal TotalWithTax { get; set; }
    }
}