using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;

namespace POS.Application.Features.Order
{
    public class OrderQuery : IRequest<ApiResponse<OrderCreateResponse>>
    {
        public int Id { get; set; }
    }

    public class OrderQueryHandler : IRequestHandler<OrderQuery, ApiResponse<OrderCreateResponse>>
    {
        private readonly IMyAppDbContext _context;

        public OrderQueryHandler(IMyAppDbContext context, ICurrentUserService currentService)
        {
            _context = context;
        }

        public async Task<ApiResponse<OrderCreateResponse>> Handle(OrderQuery request, CancellationToken cancellationToken)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.SerialNumber)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == request.Id && !o.IsDeleted, cancellationToken);
            if (order == null)
                return ApiResponse<OrderCreateResponse>.NotFound($"Order with id {request.Id} not found");
            var staff = order!.StaffId.HasValue
                ? await _context.Persons
                    .AsNoTracking()
                    .Where(p => p.Id == order.StaffId)
                    .Select(p => new TypeNamebase
                    {
                        Id = p.Id,
                        Name = p.Username ?? "N/A"
                    })
                    .FirstOrDefaultAsync(cancellationToken)
                : null;


            var orderInfo = new OrderCreateResponse
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                CustomerId = order.CustomerId,
                StaffId = order.StaffId,
                Staff = staff,
                SubTotal = order.SubTotal,
                DiscountAmount = order.DiscountAmount,
                TaxAmount = order.TaxAmount,
                TotalAmount = order.TotalAmount,
                EarnedPoints = order.EarnedPoints,
                PointsUsed = order.PointsUsed,
                CashReceived = order.CashReceived, 
                PaymentMethod = new TypeNamebase
                {
                    Id = (int)(order.PaymentMethod ?? 0),
                    Name = order.PaymentMethod.ToString() ?? "N/A"
                },
                PaymentStatus = new TypeNamebase
                {
                    Id = (int)order.PaymentStatus,
                    Name = order.PaymentStatus.ToString()
                },

                SaleType = new TypeNamebase
                {
                    Id = (int)order.SaleType,
                    Name = order.SaleType.ToString()
                },
                Status = new TypeNamebase
                {
                    Id = (int)order.Status,
                    Name = order.Status.ToString(),
                },
                Notes = order.Notes,
                OrderItems = order.OrderItems
                    .Where(oi => !oi.IsDeleted)
                    .Select(oi => new OrderItemInfo
                    {
                        Id = oi.Id,
                        OrderId = oi.OrderId,
                        ProductId = oi.ProductId,
                        ProductName = oi.Product.Name,
                        SerialNumberId = oi.SerialNumberId,
                        ImageProduct = oi.ImageProduct ?? "",
                        SerialNo = oi.SerialNumber != null ? new TypeNamebase
                        {
                            Id = (int)(oi.SerialNumberId ?? 0),
                            Name = oi.SerialNumber.SerialNo ?? "N/A"
                        } : null,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        SubTotal = oi.SubTotal,
                        WarrantyMonths = oi.WarrantyMonths,
                        WarrantyStartDate = oi.WarrantyStartDate,
                        WarrantyEndDate = oi.WarrantyEndDate,
                    }).ToList()
            };

            return ApiResponse<OrderCreateResponse>.Ok(orderInfo);
        }
    }
}