using POS.Application.Common.Typebase;

namespace POS.Application.Features.StockManagement.StockAdjustments
{
    public class StockAdjustmentInfo
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = default!;
        public string TypeAdjustment { get; set; } = default!; // "Over" or "Lost"
        public int QualityAdjustment { get; set; }
        public int QuantityBefore { get; set; }
        public int QuantityAfter { get; set; }
        public decimal CostPrice { get; set; }
        public string Reason { get; set; } = default!;
        public string? Note { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public TypeNamebase? CreatedBy { get; set; }
    }
}