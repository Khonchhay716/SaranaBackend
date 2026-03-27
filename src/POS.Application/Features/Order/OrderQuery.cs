using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;

namespace POS.Application.Features.Order
{
    public class OrderQuery : IRequest<ApiResponse<OrderInfo>>
    {
        public int Id { get; set; }
    }

    public class OrderQueryHandler : IRequestHandler<OrderQuery, ApiResponse<OrderInfo>>
    {
        private readonly IMyAppDbContext _context;

        public OrderQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<OrderInfo>> Handle(OrderQuery request, CancellationToken cancellationToken)
        {
            var order = await _context.Orders
                // .Include(o => o.Customer)
                // .Include(o => o.Staff)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SerialNumber)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == request.Id && !o.IsDeleted, cancellationToken);

            if (order == null)
            {
                return ApiResponse<OrderInfo>.NotFound($"Order with id {request.Id} not found");
            }

            var orderInfo = new OrderInfo
            {
                Id = order.Id,
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
                OrderItems = order.OrderItems
                    .Where(oi => !oi.IsDeleted)
                    .Select(oi => new OrderItemInfo
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

            return ApiResponse<OrderInfo>.Ok(orderInfo);
        }
    }
}