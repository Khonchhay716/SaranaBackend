using POS.Application.Common.Typebase;
using POS.Domain.Enums;

namespace POS.Application.Features.StockManagement.StockMovements
{
    public class StockMovementInfo
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = default!;
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? Type { get; set; }
        public string? TypeAdjustment { get; set; }
        public int Quantity { get; set; }
        public int QuantityBefore { get; set; }
        public int QuantityAfter { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Reference { get; set; }
        public string? Note { get; set; }
        public int? OrderItemId { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public TypeNamebase? CreatedBy { get; set; }
    }
}