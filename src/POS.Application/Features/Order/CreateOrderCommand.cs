// POS.Application/Features/Orders/CreateOrderCommand.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Entities.StockManagement;
using POS.Domain.Enums;
using System.Text.Json;
using DomainCustomer = POS.Domain.Entities.Customer;

namespace POS.Application.Features.Orders
{
    public record OrderItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        // ✅ Warranty - sent from UI when user selects in serial modal
        public DateTimeOffset? WarrantyStartDate { get; set; }
        public DateTimeOffset? WarrantyEndDate { get; set; }
    }

    public record CreateOrderCommand : IRequest<ApiResponse<OrderInfo>>
    {
        public int? CustomerId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? Note { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new();
    }

    public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
    {
        private readonly IMyAppDbContext _context;
        public CreateOrderCommandValidator(IMyAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.CustomerId)
                .MustAsync(async (id, ct) =>
                    id == null || id <= 0 || await _context.Customers.AnyAsync(c => c.Id == id && !c.IsDeleted, ct))
                .WithMessage("Customer not found.");

            RuleFor(x => x.PaymentMethod).IsInEnum();

            RuleFor(x => x.Items).NotEmpty().WithMessage("Order must have at least one item.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Product is required.");

                // ✅ Quantity drives both serialized and non-serialized lines now - the cashier
                // scans the product code and enters a quantity; individual serials are no longer
                // picked at sale time (they're scanned later at stock-out for serialized items).
                item.RuleFor(x => x.Quantity)
                    .GreaterThan(0).WithMessage("Quantity must be greater than 0.");

                // ✅ If has warranty end, must have start
                item.RuleFor(x => x.WarrantyStartDate)
                    .NotNull().WithMessage("Warranty start date is required when warranty end date is set.")
                    .When(x => x.WarrantyEndDate.HasValue);

                // ✅ End must be after start
                item.RuleFor(x => x.WarrantyEndDate)
                    .Must((req, end) => !end.HasValue || !req.WarrantyStartDate.HasValue || end.Value > req.WarrantyStartDate.Value)
                    .WithMessage("Warranty end date must be after start date.");
            });
        }
    }

    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, ApiResponse<OrderInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateOrderCommandHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<OrderInfo>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await ((DbContext)_context).Database.BeginTransactionAsync(cancellationToken);
            try
            {
                int? customerId = (request.CustomerId.HasValue && request.CustomerId.Value > 0)
                    ? request.CustomerId
                    : null;

                DomainCustomer? customer = null;
                if (customerId.HasValue)
                {
                    customer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.Id == customerId.Value && !c.IsDeleted, cancellationToken);
                    if (customer == null)
                        return ApiResponse<OrderInfo>.NotFound("Customer not found.");
                }

                if (request.PaymentMethod == PaymentMethod.Point && customer == null)
                    return ApiResponse<OrderInfo>.BadRequest("Customer is required for Point payment.");

                var now = DateTimeOffset.UtcNow;
                var productItems = new List<(Product, int, List<string>?, DateTimeOffset?, DateTimeOffset?)>();

                foreach (var item in request.Items)
                {
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.Id == item.ProductId && !p.IsDeleted, cancellationToken);
                    if (product == null)
                        return ApiResponse<OrderInfo>.BadRequest($"Product ID {item.ProductId} not found.");

                    if (item.Quantity <= 0)
                        return ApiResponse<OrderInfo>.BadRequest($"Quantity must be greater than 0 for {product.Name}.");

                    // ✅ Pass warranty dates from request
                    productItems.Add((product, item.Quantity, null, item.WarrantyStartDate, item.WarrantyEndDate));
                }

                var calc = await OrderCalculationService.CalculateAsync(_context, productItems, cancellationToken, now);

                // ✅ Fetch PointSetup once - reused for both redemption (payment by point)
                // and earning (non-point payment), below.
                var pointSetup = await _context.PointSetups
                    .FirstOrDefaultAsync(p => p.IsActive && !p.IsDeleted, cancellationToken);

                // ✅ FIX: convert order total ($) into POINTS using PointsPerRedemption
                // instead of comparing dollars directly against TotalPoint.
                // e.g. PointsPerRedemption = 5  =>  5 points = $1  =>  requiredPoints = TotalAmount * 5
                decimal requiredPoints = calc.TotalAmount;

                if (request.PaymentMethod == PaymentMethod.Point)
                {
                    if (pointSetup == null || pointSetup.PointsPerRedemption <= 0)
                        return ApiResponse<OrderInfo>.BadRequest("Point redemption is not configured.");

                    requiredPoints = calc.TotalAmount * pointSetup.PointsPerRedemption;

                    if (customer!.TotalPoint < requiredPoints)
                        return ApiResponse<OrderInfo>.BadRequest(
                            $"Insufficient point. Required: {requiredPoints}, Available: {customer.TotalPoint}");
                }

                // ✅ OrderNo is no longer timestamp-based; it's derived from the
                // DB-generated Id after the first save (see below).
                var order = new Order
                {
                    CustomerId = customerId,
                    Status = OrderStatus.Completed,
                    PaymentMethod = request.PaymentMethod,
                    Note = request.Note?.Trim(),
                    SubTotal = calc.SubTotal,
                    DiscountAmount = calc.DiscountAmount,
                    TotalAmount = calc.TotalAmount,
                    CreatedBy = _currentUserService.UserId,
                    CreatedDate = now,
                };

                foreach (var line in calc.Lines)
                {
                    var product = line.Product;
                    int actualQuantity;

                    if (product.ProductType == ProductType.Serialized)
                    {
                        // ✅ No serial is picked at sale time anymore - staff scans the actual
                        // unit's serial later, at stock-out, via SerialScanQuery + StockOutCommand.
                        // Here we only check enough Available stock exists so we don't oversell.
                        var availableCount = await _context.SerialStocks
                            .CountAsync(x => x.ProductId == product.Id && !x.IsDeleted && x.Status == SerialStatus.Available, cancellationToken);

                        if (availableCount < line.Quantity)
                            return ApiResponse<OrderInfo>.BadRequest($"Insufficient stock for {product.Name}. Available: {availableCount}");

                        actualQuantity = line.Quantity;
                    }
                    else
                    {
                        // Same as Serialized: only check enough Available stock exists so we
                        // don't oversell. Actual deduction happens later at stock-out
                        // (StockOutCommand), when staff confirms the hand-out.
                        var nonSerial = await _context.NonSerialStocks
                            .FirstOrDefaultAsync(x => x.ProductId == product.Id && !x.IsDeleted, cancellationToken);

                        if (nonSerial == null || nonSerial.Quantity < line.Quantity)
                            return ApiResponse<OrderInfo>.BadRequest($"Insufficient stock for {product.Name}. Available: {nonSerial?.Quantity ?? 0}");

                        actualQuantity = line.Quantity;
                    }

                    order.Items.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = actualQuantity,
                        UnitPrice = product.SalePrice,
                        DiscountAmount = line.SpecificDiscount,
                        LineTotal = line.LineTotal,
                        DiscountId = line.SpecificDiscountApplied?.Id,
                        // ✅ Filled in later at stock-out time for serialized products.
                        SerialNumbers = null,
                        // ✅ Warranty from request (set by user in serial modal)
                        WarrantyStartDate = line.WarrantyStartDate,
                        WarrantyEndDate = line.WarrantyEndDate,
                        CreatedBy = _currentUserService.UserId,
                        CreatedDate = now,
                    });
                }

                if (customer != null)
                {
                    if (request.PaymentMethod == PaymentMethod.Point)
                    {
                        // ✅ FIX: deduct actual POINTS (already converted above using
                        // PointsPerRedemption), not the raw dollar TotalAmount.
                        customer.TotalPoint -= requiredPoints;
                        order.PointUsed = requiredPoints;
                    }
                    else
                    {
                        // reuse pointSetup fetched above - no duplicate query needed
                        if (pointSetup != null && calc.TotalAmount >= pointSetup.MinOrderAmount && pointSetup.PointValue > 0)
                        {
                            var earnedPoint = calc.TotalAmount * pointSetup.PointValue;
                            if (pointSetup.MaxPointPerOrder.HasValue)
                                earnedPoint = Math.Min(earnedPoint, pointSetup.MaxPointPerOrder.Value);

                            order.PointEarned = earnedPoint;
                            customer.TotalPoint += earnedPoint;
                        }
                    }
                }

                _context.Orders.Add(order);

                // ✅ First save: lets the DB generate order.Id (identity/auto-increment).
                await _context.SaveChangesAsync(cancellationToken);

                // ✅ Now that we have the real Id, build OrderNo from it, e.g. ORD-0000001.
                // D7 = 7-digit zero padding. Increase to D8/D9 etc. if you expect more orders.
                order.OrderNo = $"ORD-{order.Id:D7}";

                // ✅ Second save: persists the OrderNo we just computed.
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                var result = await _context.Orders
                    .AsNoTracking()
                    .Include(o => o.Customer)
                    .Include(o => o.Items).ThenInclude(i => i.Product)
                    .Include(o => o.Items).ThenInclude(i => i.Discount)
                    .FirstOrDefaultAsync(o => o.Id == order.Id, cancellationToken);

                return ApiResponse<OrderInfo>.Created(MapToInfo(result!), "Order created successfully.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        internal static OrderInfo MapToInfo(Order o) => new()
        {
            Id = o.Id,
            OrderNo = o.OrderNo,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer != null ? $"{o.Customer.FirstName} {o.Customer.LastName}" : "Walk-in Customer",
            Status = o.Status.ToString(),
            PaymentMethod = o.PaymentMethod.ToString(),
            SubTotal = o.SubTotal,
            DiscountAmount = o.DiscountAmount,
            TotalAmount = o.TotalAmount,
            PointEarned = o.PointEarned,
            PointUsed = o.PointUsed,
            Note = o.Note,
            CreatedDate = o.CreatedDate,
            Items = o.Items.Select(i => new OrderItemInfo
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountAmount = i.DiscountAmount,
                LineTotal = i.LineTotal,
                DiscountName = i.Discount?.Name,
                SerialNumbers = !string.IsNullOrEmpty(i.SerialNumbers)
                    ? JsonSerializer.Deserialize<List<string>>(i.SerialNumbers)
                    : null,
                FulfilledDate = i.FulfilledDate,
                // ✅ Warranty from OrderItem
                WarrantyDays = i.WarrantyStartDate.HasValue && i.WarrantyEndDate.HasValue
                    ? (int?)(i.WarrantyEndDate.Value - i.WarrantyStartDate.Value).Days
                    : null,
                WarrantyStartDate = i.WarrantyStartDate,
                WarrantyEndDate = i.WarrantyEndDate,
            }).ToList()
        };
    }
}