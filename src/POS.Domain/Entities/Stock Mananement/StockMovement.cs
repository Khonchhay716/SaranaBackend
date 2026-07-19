using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities.StockManagement
{
    public class StockMovement : BaseEntity
    {
        public MovementType Type { get; set; }
        public int Quantity { get; set; }
        public int QuantityBefore { get; set; }
        public int QuantityAfter { get; set; }
        public TypeAdjustment? TypeAdjustment { get; set; } = null;
        public string? Reference { get; set; }
        public string? Note { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public int ProductId { get; set; }
        public int? SupplierId { get; set; }

        // ✅ Links a stock-out movement back to the order line it fulfills (staff scanning
        // serials to hand out a sold product). Null for stock-outs unrelated to a sale.
        public int? OrderItemId { get; set; }

        public Product Product { get; set; } = default!;
        public Supplier? Supplier { get; set; }
        public OrderItem? OrderItem { get; set; }
    }
}