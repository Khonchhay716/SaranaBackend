// POS.Domain/Entities/OrderItem.cs
using POS.Domain.Common;

namespace POS.Domain.Entities
{
    public class OrderItem : BaseAuditableEntity
    {
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int ProductId { get; set; }
        public StockManagement.Product Product { get; set; } = null!;

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LineTotal { get; set; }

        public int? DiscountId { get; set; }
        public Discount? Discount { get; set; }

        public string? SerialNumbers { get; set; }
        public DateTimeOffset? WarrantyStartDate { get; set; }
        public DateTimeOffset? WarrantyEndDate { get; set; }
    }
}