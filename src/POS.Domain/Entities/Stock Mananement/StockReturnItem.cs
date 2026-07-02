using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities.StockManagement
{
    public class StockReturnItem : BaseEntity
    {
        public int StockReturnId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public ReturnReason Reason { get; set; }
        public string? Note { get; set; }
        public string? SerialNumbers { get; set; }

        // Navigation
        public StockReturn StockReturn { get; set; } = default!;
        public Product Product { get; set; } = default!;
    }
}