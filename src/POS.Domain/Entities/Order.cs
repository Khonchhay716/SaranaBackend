using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities
{
    public class Order : BaseAuditableEntity
    {
        public string OrderNumber { get; set; } = string.Empty;
        public DateTimeOffset OrderDate { get; set; }
        public int? CustomerId { get; set; }
        public int? StaffId { get; set; }
        public Person? Staff { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; } = 0m;
        public decimal? TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public SaleType SaleType { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public PaymentMethodCode? PaymentMethod { get; set; }
        public string? Notes { get; set; }
        public int EarnedPoints { get; set; } = 0;
        public decimal CashReceived { get; set; } = 0; 
        public int PointsUsed { get; set; } = 0;
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    public class OrderItem : BaseAuditableEntity
    {
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int? SerialNumberId { get; set; }
        public string? ImageProduct { get; set; }
        public SerialNumber? SerialNumber { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; } = 0m;
        public string? Notes { get; set; }
        public int? WarrantyMonths { get; set; }
        public DateTimeOffset? WarrantyStartDate { get; set; }
        public DateTimeOffset? WarrantyEndDate { get; set; }
    }
}