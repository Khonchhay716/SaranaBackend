using POS.Domain.Common;
using POS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace POS.Domain.Entities
{
    public class Order : BaseAuditableEntity
    {
        // Order Info
        public string OrderNumber { get; set; } = string.Empty;
        public DateTimeOffset OrderDate { get; set; }
        public int? CustomerId { get; set; }
        // public Customer? Customer { get; set; }

        public int? StaffId { get; set; }
        // public Person? Staff { get; set; }
        public decimal SubTotal { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        // Status
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public SaleType SaleType { get; set; } = SaleType.POS;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public PaymentMethodCode? PaymentMethod { get; set; }
        public string? Notes { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }


    public class OrderItem : BaseAuditableEntity
    {
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int? SerialNumberId { get; set; }
        public SerialNumber? SerialNumber { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }

        public string? Notes { get; set; }
    }
}