using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;

namespace POS.Application.Features.Orders
{
    // ✅ Lists an order's lines that are paid but not yet handed out/confirmed
    // (Serialized: no serial assigned yet; Non-Serialized: not yet stock-out confirmed)
    // - staff looks this up by Order No before scanning/confirming.
    public record OrderPendingItemsQuery : IRequest<ApiResponse<List<PendingOrderItemInfo>>>
    {
        public string OrderNo { get; set; } = default!;
    }

    public class OrderPendingItemsQueryValidator : AbstractValidator<OrderPendingItemsQuery>
    {
        public OrderPendingItemsQueryValidator()
        {
            RuleFor(x => x.OrderNo).NotEmpty().WithMessage("Order No is required.");
        }
    }

    public class PendingOrderItemInfo
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = default!;
        public string ProductName { get; set; } = default!;
        public int Quantity { get; set; }

        // ✅ Tells the client whether to show the serial-scan UI (true) or a plain
        // quantity-confirm action (false) when calling StockOutCommand for this line.
        public bool RequiresSerial { get; set; }
    }

    public class OrderPendingItemsQueryHandler : IRequestHandler<OrderPendingItemsQuery, ApiResponse<List<PendingOrderItemInfo>>>
    {
        private readonly IMyAppDbContext _context;
        public OrderPendingItemsQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<PendingOrderItemInfo>>> Handle(OrderPendingItemsQuery request, CancellationToken cancellationToken)
        {
            var orderNo = request.OrderNo.Trim();

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.OrderNo == orderNo, cancellationToken);

            if (order == null)
                return ApiResponse<List<PendingOrderItemInfo>>.NotFound($"Order '{orderNo}' not found.");

            var pending = order.Items
                .Where(i => !i.FulfilledDate.HasValue)
                .Select(i => new PendingOrderItemInfo
                {
                    OrderItemId = i.Id,
                    ProductId = i.ProductId,
                    ProductCode = i.Product.Code ?? string.Empty,
                    ProductName = i.Product.Name,
                    Quantity = i.Quantity,
                    RequiresSerial = i.Product.ProductType == ProductType.Serialized,
                })
                .ToList();

            return ApiResponse<List<PendingOrderItemInfo>>.Ok(pending, "Pending items retrieved.");
        }
    }
}
