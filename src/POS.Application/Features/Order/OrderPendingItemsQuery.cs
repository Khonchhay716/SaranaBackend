using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;

namespace POS.Application.Features.Orders
{
    // ✅ Lists an order's serialized lines that are paid but not yet handed out
    // (no serial assigned yet) - staff looks this up by Order No before scanning.
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
                .Where(i => i.Product.ProductType == ProductType.Serialized && string.IsNullOrEmpty(i.SerialNumbers))
                .Select(i => new PendingOrderItemInfo
                {
                    OrderItemId = i.Id,
                    ProductId = i.ProductId,
                    ProductCode = i.Product.Code ?? string.Empty,
                    ProductName = i.Product.Name,
                    Quantity = i.Quantity,
                })
                .ToList();

            return ApiResponse<List<PendingOrderItemInfo>>.Ok(pending, "Pending items retrieved.");
        }
    }
}
