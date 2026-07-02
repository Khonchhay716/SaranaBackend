// POS.Domain/Entities/Order.cs
using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities
{
    public class Order : BaseAuditableEntity
    {
        public string OrderNo { get; set; } = string.Empty;

        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PointEarned { get; set; }
        public decimal PointUsed { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Completed;
        public PaymentMethod PaymentMethod { get; set; }
        public string? Note { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}