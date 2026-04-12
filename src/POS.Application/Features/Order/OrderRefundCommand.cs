using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;

namespace POS.Application.Features.Order
{
    public record OrderRefundCommand : IRequest<ApiResponse<bool>>
    {
        public int    Id     { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class OrderRefundCommandHandler : IRequestHandler<OrderRefundCommand, ApiResponse<bool>>
    {
        private readonly IMyAppDbContext _context;

        public OrderRefundCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<bool>> Handle(
            OrderRefundCommand request,
            CancellationToken  cancellationToken)
        {
            // ==================== Get Order ====================
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == request.Id && !o.IsDeleted, cancellationToken);

            if (order == null)
                return ApiResponse<bool>.NotFound($"Order with id {request.Id} not found.");

            // ==================== Validate ====================
            if (order.PaymentStatus == PaymentStatus.Refunded)
                return ApiResponse<bool>.BadRequest("Order has already been refunded.");

            if (order.PaymentStatus == PaymentStatus.Pending)
                return ApiResponse<bool>.BadRequest("Cannot refund an unpaid order.");

            // ==================== Reverse Stock ====================
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.SerialNumber)
                .Where(oi => oi.OrderId == order.Id && !oi.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var item in orderItems)
            {
                if (item.Product == null) continue;

                if (item.SerialNumber != null)
                {
                    // ✅ Serial: return to Available
                    item.SerialNumber.Status   = "Available";
                    item.SerialNumber.SoldDate = null;
                    item.Product.Stock        += 1;

                    // ✅ Fix duplicate key — release unique constraint
                    item.SerialNumberId = null;
                }
                else
                {
                    // ✅ Normal product — return quantity
                    item.Product.Stock += item.Quantity;
                }
            }

            // ==================== Reverse Customer Point ====================
            if (order.CustomerId.HasValue)
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == order.CustomerId.Value && !c.IsDeleted, cancellationToken);

                if (customer != null)
                {
                    // ✅ Remove earned points (Cash/QR order)
                    if (order.EarnedPoints > 0)
                        customer.TotalPoint = Math.Max(0, customer.TotalPoint - order.EarnedPoints);

                    // ✅ Return spent points (Point order)
                    if (order.PointsUsed > 0)
                        customer.TotalPoint += order.PointsUsed;
                }
            }

            // ==================== Update Order ====================
            order.PaymentStatus = PaymentStatus.Refunded;
            order.Notes         = string.IsNullOrWhiteSpace(request.Reason)
                ? order.Notes
                : $"{order.Notes} | Refund: {request.Reason}".Trim(' ', '|');

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.Ok(true, "Order refunded successfully.");
        }
    }
}