using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities.StockManagement
{
    public class StockAdjustment : BaseEntity
    {
        public int ProductId { get; set; }
        public TypeAdjustment TypeAdjustment { get; set; }
        public int QualityAdjustment { get; set; }
        public int QuantityBefore { get; set; }
        public int QuantityAfter { get; set; }
        public decimal CostPrice { get; set; }
        public AdjustmentReason Reason { get; set; }
        public string? Note { get; set; }

        // Navigation
        public Product Product { get; set; } = default!;
    }
}