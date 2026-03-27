using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using POS.Domain.Enums;
using DomainOrder = POS.Domain.Entities.Order;
using DomainOrderItem = POS.Domain.Entities.OrderItem;

namespace POS.Application.Features.Order
{
    public class OrderItemCreateDto
    {
        public int ProductId { get; set; }
        public int? SerialNumberId { get; set; } // Optional - for products with serial numbers
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Notes { get; set; }
    }
    
    public record OrderCreateCommand : IRequest<ApiResponse<OrderInfo>>
    {
        public int? CustomerId { get; set; }
        public int? StaffId { get; set; }
        public decimal? DiscountAmount { get; set; }
        public SaleType SaleType { get; set; } = SaleType.POS;
        public PaymentMethodCode? PaymentMethod { get; set; }
        public string? Notes { get; set; }
        
        public List<OrderItemCreateDto> Items { get; set; } = new List<OrderItemCreateDto>();
    }
    
    public class OrderCreateCommandValidator : AbstractValidator<OrderCreateCommand>
    {
        public OrderCreateCommandValidator()
        {
            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Order must have at least one item");
            
            RuleFor(x => x.DiscountAmount)
                .GreaterThanOrEqualTo(0).When(x => x.DiscountAmount.HasValue);
            
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.ProductId).GreaterThan(0);
                item.RuleFor(x => x.Quantity).GreaterThan(0);
                item.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
            });
        }
    }

    public class OrderCreateCommandHandler : IRequestHandler<OrderCreateCommand, ApiResponse<OrderInfo>>
    {
        private readonly IMyAppDbContext _context;

        public OrderCreateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<OrderInfo>> Handle(OrderCreateCommand request, CancellationToken cancellationToken)
        {
            var validator = new OrderCreateCommandValidator();
            var validationResult = validator.Validate(request);

            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return ApiResponse<OrderInfo>.BadRequest(errorMessages);
            }

            // ✅ VALIDATE: Customer exists (if provided)
            // if (request.CustomerId.HasValue)
            // {
            //     var customerExists = await _context.Customers
            //         .AnyAsync(c => c.Id == request.CustomerId.Value && !c.IsDeleted, cancellationToken);
                
            //     if (!customerExists)
            //     {
            //         return ApiResponse<OrderInfo>.NotFound($"Customer with id {request.CustomerId.Value} not found");
            //     }
            // }

            // ✅ VALIDATE: Staff exists (if provided)
            // if (request.StaffId.HasValue)
            // {
            //     var staffExists = await _context.Users
            //         .AnyAsync(u => u.Id == request.StaffId.Value && !u.IsDeleted, cancellationToken);
                
            //     if (!staffExists)
            //     {
            //         return ApiResponse<OrderInfo>.NotFound($"Staff with id {request.StaffId.Value} not found");
            //     }
            // }

            // ✅ VALIDATE: Products and Serial Numbers
            decimal subtotal = 0;
            var orderItems = new List<DomainOrderItem>();

            foreach (var item in request.Items)
            {
                var product = await _context.Products
                    .Include(p => p.SerialNumbers.Where(s => !s.IsDeleted))
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId && !p.IsDeleted, cancellationToken);

                if (product == null)
                {
                    return ApiResponse<OrderInfo>.NotFound($"Product with id {item.ProductId} not found");
                }

                // Check serial number if provided
                if (item.SerialNumberId.HasValue)
                {
                    var serialNumber = product.SerialNumbers
                        .FirstOrDefault(s => s.Id == item.SerialNumberId.Value);

                    if (serialNumber == null)
                    {
                        return ApiResponse<OrderInfo>.NotFound(
                            $"Serial number with id {item.SerialNumberId.Value} not found for product {product.Name}");
                    }

                    if (serialNumber.Status != "Available")
                    {
                        return ApiResponse<OrderInfo>.BadRequest(
                            $"Serial number {serialNumber.SerialNo} is not available (Status: {serialNumber.Status})");
                    }

                    // Mark serial number as Sold
                    serialNumber.Status = "Sold";
                    serialNumber.SoldDate = DateTimeOffset.UtcNow;
                }
                else
                {
                    // Check stock for products without serial numbers
                    if (product.Stock < item.Quantity)
                    {
                        return ApiResponse<OrderInfo>.BadRequest(
                            $"Insufficient stock for product {product.Name}. Available: {product.Stock}, Requested: {item.Quantity}");
                    }
                    
                    // Reduce stock
                    product.Stock -= item.Quantity;
                }

                // Calculate item subtotal
                var itemSubtotal = item.Quantity * item.UnitPrice;
                subtotal += itemSubtotal;

                // Create order item
                orderItems.Add(new DomainOrderItem
                {
                    ProductId = item.ProductId,
                    SerialNumberId = item.SerialNumberId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    SubTotal = itemSubtotal,
                    Notes = item.Notes
                });
            }

            // ✅ Calculate totals
            var taxAmount = subtotal * 0.10m; // 10% VAT
            var totalAmount = subtotal + taxAmount - (request.DiscountAmount ?? 0);

            // ✅ Create Order
            var order = new DomainOrder
            {
                OrderNumber = $"ORD-{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                OrderDate = DateTimeOffset.UtcNow,
                CustomerId = request.CustomerId,
                StaffId = request.StaffId,
                SubTotal = subtotal,
                DiscountAmount = request.DiscountAmount,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount,
                Status = OrderStatus.Completed,
                SaleType = request.SaleType,
                PaymentStatus = request.PaymentMethod.HasValue ? PaymentStatus.Paid : PaymentStatus.Pending,
                PaymentMethod = request.PaymentMethod,
                Notes = request.Notes,
                OrderItems = orderItems
            };

            _context.Orders.Add(order);

            // Update stock for products with serial numbers
            foreach (var item in request.Items.Where(i => i.SerialNumberId.HasValue))
            {
                var product = await _context.Products
                    .Include(p => p.SerialNumbers)
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken);
                
                if (product != null)
                {
                    product.Stock = product.SerialNumbers.Count(s => !s.IsDeleted && s.Status == "Available");
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            // ✅ Return response
            var orderInfo = await GetOrderInfo(order.Id, cancellationToken);
            
            return ApiResponse<OrderInfo>.Created(orderInfo, "Order created successfully");
        }

        private async Task<OrderInfo> GetOrderInfo(int orderId, CancellationToken cancellationToken)
        {
            var order = await _context.Orders
                // .Include(o => o.Customer)
                // .Include(o => o.Staff)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SerialNumber)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            return new OrderInfo
            {
                Id = order!.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                CustomerId = order.CustomerId,
                // Customer = order.Customer != null ? new TypeNamebase { Id = order.Customer.Id, Name = order.Customer.Name } : null,
                StaffId = order.StaffId,
                // Staff = order.Staff != null ? new TypeNamebase { Id = order.Staff.Id, Name = order.Staff.Username } : null,
                SubTotal = order.SubTotal,
                DiscountAmount = order.DiscountAmount,
                TaxAmount = order.TaxAmount,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                StatusName = order.Status.ToString(),
                SaleType = order.SaleType,
                SaleTypeName = order.SaleType.ToString(),
                PaymentStatus = order.PaymentStatus,
                PaymentStatusName = order.PaymentStatus.ToString(),
                PaymentMethod = order.PaymentMethod,
                PaymentMethodName = order.PaymentMethod?.ToString(),
                Notes = order.Notes,
                CreatedDate = order.CreatedDate,
                CreatedBy = order.CreatedBy,
                OrderItems = order.OrderItems.Select(oi => new OrderItemInfo
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.Name,
                    ProductSKU = oi.Product.SKU,
                    SerialNumberId = oi.SerialNumberId,
                    SerialNo = oi.SerialNumber?.SerialNo,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    SubTotal = oi.SubTotal,
                    Notes = oi.Notes
                }).ToList()
            };
        }
    }
}