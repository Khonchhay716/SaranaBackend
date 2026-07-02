// POS.Application/Features/Orders/OrderCalculationService.cs
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Entities.StockManagement;
using DomainDiscount = POS.Domain.Entities.Discount;

namespace POS.Application.Features.Orders
{
    public class OrderLineCalculation
    {
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; }
        public List<string>? SerialNumbers { get; set; }
        public decimal LineSubTotal { get; set; }
        public decimal SpecificDiscount { get; set; }
        public decimal LineAfterSpecific { get; set; }
        public decimal GlobalDiscountShare { get; set; }
        public decimal LineTotal { get; set; }
        public DomainDiscount? SpecificDiscountApplied { get; set; }

        // ✅ Warranty - passed from request, NOT from product
        public DateTimeOffset? WarrantyStartDate { get; set; }
        public DateTimeOffset? WarrantyEndDate { get; set; }
    }

    public class OrderCalculationResult
    {
        public List<OrderLineCalculation> Lines { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public DomainDiscount? GlobalDiscountApplied { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    public static class OrderCalculationService
    {
        // ✅ Updated tuple to include warranty dates
        public static async Task<OrderCalculationResult> CalculateAsync(
            IMyAppDbContext context,
            List<(Product Product, int Quantity, List<string>? SerialNumbers, DateTimeOffset? WarrantyStart, DateTimeOffset? WarrantyEnd)> items,
            CancellationToken cancellationToken,
            DateTimeOffset? orderDate = null)
        {
            var now = orderDate ?? DateTimeOffset.UtcNow;

            var activeDiscounts = await context.Discounts
                .Where(d => d.IsActive && !d.IsDeleted
                    && (d.StartDate == null || d.StartDate <= now)
                    && (d.EndDate == null || d.EndDate >= now))
                .Include(d => d.ProductDiscounts.Where(pd => !pd.IsDeleted))
                .ToListAsync(cancellationToken);

            var globalDiscounts = activeDiscounts.Where(d => d.IsAllProducts).ToList();
            var productDiscountMap = activeDiscounts
                .Where(d => !d.IsAllProducts)
                .SelectMany(d => d.ProductDiscounts.Select(pd => new { pd.ProductId, Discount = d }))
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Discount).ToList());

            var result = new OrderCalculationResult();

            var lineSubTotals = items.Select(i => i.Product.SalePrice * i.Quantity).ToList();

            var lines = new List<OrderLineCalculation>();
            for (int idx = 0; idx < items.Count; idx++)
            {
                var (product, quantity, serialNumbers, warrantyStart, warrantyEnd) = items[idx];
                var lineSubTotal = lineSubTotals[idx];

                decimal specificDiscount = 0;
                DomainDiscount? specificApplied = null;

                if (productDiscountMap.TryGetValue(product.Id, out var specificCandidates))
                {
                    foreach (var discount in specificCandidates)
                    {
                        if (discount.MinOrderAmount.HasValue && lineSubTotal < discount.MinOrderAmount.Value)
                            continue;

                        var calculated = discount.Type == "Percentage"
                            ? lineSubTotal * (discount.Value / 100m)
                            : Math.Min(discount.Value, lineSubTotal);

                        if (calculated > specificDiscount)
                        {
                            specificDiscount = calculated;
                            specificApplied = discount;
                        }
                    }
                }

                var lineAfterSpecific = lineSubTotal - specificDiscount;

                lines.Add(new OrderLineCalculation
                {
                    Product = product,
                    Quantity = quantity,
                    SerialNumbers = serialNumbers,
                    LineSubTotal = lineSubTotal,
                    SpecificDiscount = specificDiscount,
                    LineAfterSpecific = lineAfterSpecific,
                    SpecificDiscountApplied = specificApplied,
                    // ✅ Pass through warranty from request
                    WarrantyStartDate = warrantyStart,
                    WarrantyEndDate = warrantyEnd,
                });
            }

            var orderTotalAfterSpecific = lines.Sum(l => l.LineAfterSpecific);

            decimal globalDiscountAmount = 0;
            DomainDiscount? globalApplied = null;

            foreach (var discount in globalDiscounts)
            {
                if (discount.MinOrderAmount.HasValue && orderTotalAfterSpecific < discount.MinOrderAmount.Value)
                    continue;

                var calculated = discount.Type == "Percentage"
                    ? orderTotalAfterSpecific * (discount.Value / 100m)
                    : Math.Min(discount.Value, orderTotalAfterSpecific);

                if (calculated > globalDiscountAmount)
                {
                    globalDiscountAmount = calculated;
                    globalApplied = discount;
                }
            }

            foreach (var line in lines)
            {
                var proportion = orderTotalAfterSpecific > 0 ? line.LineAfterSpecific / orderTotalAfterSpecific : 0;
                line.GlobalDiscountShare = Math.Round(globalDiscountAmount * proportion, 2);
                line.LineTotal = line.LineAfterSpecific - line.GlobalDiscountShare;

                result.SubTotal += line.LineSubTotal;
                result.DiscountAmount += line.SpecificDiscount + line.GlobalDiscountShare;
            }

            result.Lines = lines;
            result.GlobalDiscountApplied = globalApplied;
            result.TotalAmount = result.SubTotal - result.DiscountAmount;

            // Warnings
            foreach (var line in lines)
            {
                if (line.SpecificDiscountApplied != null) continue;

                if (productDiscountMap.TryGetValue(line.Product.Id, out var specificCandidates))
                {
                    foreach (var discount in specificCandidates.Where(d => d.MinOrderAmount.HasValue && d.MinOrderAmount.Value > 0))
                    {
                        var progress = (line.LineSubTotal / discount.MinOrderAmount!.Value) * 100m;
                        if (progress >= 70 && progress < 100)
                        {
                            var remaining = discount.MinOrderAmount.Value - line.LineSubTotal;
                            var valueText = discount.Type == "Percentage" ? $"{discount.Value}%" : $"${discount.Value}";
                            result.Warnings.Add(
                                $"{line.Product.Name}: order is at {progress:0.##}% of minimum amount (${discount.MinOrderAmount.Value}) " +
                                $"required for \"{discount.Name}\" discount ({valueText} off). Add ${remaining:0.##} more to qualify.");
                        }
                    }
                }
            }

            if (globalApplied == null)
            {
                foreach (var discount in globalDiscounts.Where(d => d.MinOrderAmount.HasValue && d.MinOrderAmount.Value > 0))
                {
                    var progress = (orderTotalAfterSpecific / discount.MinOrderAmount!.Value) * 100m;
                    if (progress >= 70 && progress < 100)
                    {
                        var remaining = discount.MinOrderAmount.Value - orderTotalAfterSpecific;
                        var valueText = discount.Type == "Percentage" ? $"{discount.Value}%" : $"${discount.Value}";
                        result.Warnings.Add(
                            $"Order: order is at {progress:0.##}% of minimum amount (${discount.MinOrderAmount.Value}) " +
                            $"required for \"{discount.Name}\" discount ({valueText} off). Add ${remaining:0.##} more to qualify.");
                    }
                }
            }

            return result;
        }
    }
}