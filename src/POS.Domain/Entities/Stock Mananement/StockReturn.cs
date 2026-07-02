using POS.Domain.Common;
using POS.Domain.Enums;
namespace POS.Domain.Entities.StockManagement
{
    public class StockReturn : BaseEntity
    {
        public string ReturnNo { get; set; } = default!;
        public int SupplierId { get; set; }
        public string? Note { get; set; }
        public decimal TotalAmount { get; set; }
        public ReturnStatus Status { get; set; } = ReturnStatus.Draft;
        // Navigation
        public Supplier Supplier { get; set; } = default!;
        public ICollection<StockReturnItem> Items { get; set; } = new List<StockReturnItem>();
    }
}