// POS.Application/Features/Orders/OrderSummaryQuery.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;
using POS.Domain.Entities.StockManagement;

namespace POS.Application.Features.Orders
{
    public record OrderSummaryItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public List<string>? SerialNumbers { get; set; }

        // ✅ Warranty from UI
        public DateTimeOffset? WarrantyStartDate { get; set; }
        public DateTimeOffset? WarrantyEndDate { get; set; }
    }

    public record OrderSummaryQuery : IRequest<ApiResponse<OrderSummaryInfo>>
    {
        public int? CustomerId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public List<OrderSummaryItemRequest> Items { get; set; } = new();
    }

    public class OrderSummaryQueryValidator : AbstractValidator<OrderSummaryQuery>
    {
        private readonly IMyAppDbContext _context;

        public OrderSummaryQueryValidator(IMyAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.Items).NotEmpty().WithMessage("Order must have at least one item.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.ProductId).GreaterThan(0);

                item.RuleFor(x => x.Quantity)
                    .GreaterThan(0)
                    .WhenAsync(async (req, ct) =>
                    {
                        var p = await _context.Products.FindAsync(new object[] { req.ProductId }, ct);
                        return p == null || p.ProductType != ProductType.Serialized;
                    });
            });
        }
    }

    public class OrderSummaryQueryHandler : IRequestHandler<OrderSummaryQuery, ApiResponse<OrderSummaryInfo>>
    {
        private readonly IMyAppDbContext _context;
        public OrderSummaryQueryHandler(IMyAppDbContext context) => _context = context;

        public async Task<ApiResponse<OrderSummaryInfo>> Handle(OrderSummaryQuery request, CancellationToken cancellationToken)
        {
            var customerId = (request.CustomerId.HasValue && request.CustomerId.Value > 0)
                ? request.CustomerId
                : null;

            // ✅ Include warranty dates in tuple
            var items = new List<(Product, int, List<string>?, DateTimeOffset?, DateTimeOffset?)>();

            foreach (var item in request.Items)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId && !p.IsDeleted, cancellationToken);
                if (product == null)
                    return ApiResponse<OrderSummaryInfo>.BadRequest($"Product ID {item.ProductId} not found.");

                int quantity = product.ProductType == ProductType.Serialized
                    ? (item.SerialNumbers?.Count ?? 0)
                    : item.Quantity;

                if (quantity <= 0)
                    return ApiResponse<OrderSummaryInfo>.BadRequest($"Quantity must be greater than 0 for {product.Name}.");

                if (product.ProductType == ProductType.Serialized)
                {
                    var requestedSet = item.SerialNumbers!.Select(s => s.Trim()).ToHashSet();

                    var foundSet = await _context.SerialStocks
                        .Where(x => requestedSet.Contains(x.SerialNo)
                            && x.ProductId == product.Id
                            && !x.IsDeleted
                            && x.Status == SerialStatus.Available)
                        .Select(x => x.SerialNo)
                        .ToListAsync(cancellationToken);

                    var missingSerials = requestedSet.Except(foundSet).ToList();
                    if (missingSerials.Any())
                        return ApiResponse<OrderSummaryInfo>.BadRequest(
                            $"Serials not found or unavailable for {product.Name}: {string.Join(", ", missingSerials)}");
                }
                else
                {
                    var nonSerial = await _context.NonSerialStocks
                        .FirstOrDefaultAsync(x => x.ProductId == product.Id && !x.IsDeleted, cancellationToken);

                    if (nonSerial == null || nonSerial.Quantity < quantity)
                        return ApiResponse<OrderSummaryInfo>.BadRequest(
                            $"Insufficient stock for {product.Name}. Available: {nonSerial?.Quantity ?? 0}");
                }

                // ✅ Pass warranty dates from request
                items.Add((product, quantity, item.SerialNumbers, item.WarrantyStartDate, item.WarrantyEndDate));
            }

            var calc = await OrderCalculationService.CalculateAsync(_context, items, cancellationToken);

            decimal? customerPoint = null;
            string customerName = "Walk-in Customer";

            if (customerId.HasValue)
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == customerId.Value && !c.IsDeleted, cancellationToken);
                if (customer == null)
                    return ApiResponse<OrderSummaryInfo>.NotFound("Customer not found.");

                customerPoint = customer.TotalPoint;
                customerName = $"{customer.FirstName} {customer.LastName}";
            }

            decimal pointEarned = 0;
            var pointSetup = await _context.PointSetups.FirstOrDefaultAsync(p => p.IsActive && !p.IsDeleted, cancellationToken);
            if (pointSetup != null && customerId.HasValue && calc.TotalAmount >= pointSetup.MinOrderAmount && pointSetup.PointValue > 0)
            {
                pointEarned = calc.TotalAmount * pointSetup.PointValue;
                if (pointSetup.MaxPointPerOrder.HasValue)
                    pointEarned = Math.Min(pointEarned, pointSetup.MaxPointPerOrder.Value);
            }

            if (request.PaymentMethod == PaymentMethod.Point)
            {
                if (!customerPoint.HasValue)
                    return ApiResponse<OrderSummaryInfo>.BadRequest("Customer is required for Point payment.");
                if (customerPoint.Value < calc.TotalAmount)
                    return ApiResponse<OrderSummaryInfo>.BadRequest(
                        $"Insufficient point. Required: {calc.TotalAmount}, Available: {customerPoint.Value}");
            }

            var res = new OrderSummaryInfo
            {
                CustomerId = customerId,
                CustomerName = customerName,
                CustomerAvailablePoint = customerPoint,
                PaymentMethod = request.PaymentMethod.ToString(),
                SubTotal = calc.SubTotal,
                DiscountAmount = calc.DiscountAmount,
                TotalAmount = calc.TotalAmount,
                PointEarned = pointEarned,
                Warnings = calc.Warnings,
                Items = calc.Lines.Select(l => new OrderItemInfo
                {
                    ProductId = l.Product.Id,
                    ProductName = l.Product.Name,
                    Quantity = l.Quantity,
                    UnitPrice = l.Product.SalePrice,
                    DiscountAmount = l.SpecificDiscount,
                    DiscountName = l.SpecificDiscountApplied?.Name,
                    GlobalDiscountAmount = l.GlobalDiscountShare,
                    GlobalDiscountName = calc.GlobalDiscountApplied?.Name,
                    LineTotal = l.LineAfterSpecific,
                    SerialNumbers = l.SerialNumbers,
                    // ✅ Warranty from request
                    WarrantyDays = l.WarrantyStartDate.HasValue && l.WarrantyEndDate.HasValue
                        ? (int?)(l.WarrantyEndDate.Value - l.WarrantyStartDate.Value).Days
                        : null,
                    WarrantyStartDate = l.WarrantyStartDate,
                    WarrantyEndDate = l.WarrantyEndDate,
                }).ToList()
            };

            return ApiResponse<OrderSummaryInfo>.Ok(res, "Order summary calculated.");
        }
    }
}