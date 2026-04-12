using POS.Application.Common.Typebase;
using POS.Domain.Enums;

namespace POS.Application.Features.Order
{
    public class OrderListResponse
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTimeOffset OrderDate { get; set; }
        public int? CustomerId { get; set; }
        public int? StaffId { get; set; }
        public TypeNamebase? Staff { get; set; }
        public decimal SubTotal { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public TypeNamebase? Status { get; set; }
        public TypeNamebase? SaleType { get; set; }
        public TypeNamebase? PaymentStatus { get; set; }
        public TypeNamebase? PaymentMethod { get; set; }
        public string? Notes { get; set; }
        public int EarnedPoints { get; set; }
        public int PointsUsed { get; set; }
        public decimal CashReceived { get; set; }
        public List<OrderItemInfo> OrderItems { get; set; } = new();
    }

    public class OrderCreateResponse
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTimeOffset OrderDate { get; set; }
        public int? CustomerId { get; set; }
        public TypeNamebase? Customer { get; set; }
        public int? StaffId { get; set; }
        public TypeNamebase? Staff { get; set; }
        public decimal SubTotal { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public TypeNamebase? Status { get; set; }
        public TypeNamebase? SaleType { get; set; }
        public TypeNamebase? PaymentStatus { get; set; }
        public TypeNamebase? PaymentMethod { get; set; }
        public string? Notes { get; set; }
        public int EarnedPoints { get; set; }
        public int PointsUsed { get; set; }
        public decimal CashReceived { get; set; }
        public List<OrderItemInfo> OrderItems { get; set; } = new();
    }

    public class OrderItemInfo
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ImageProduct { get; set; } = string.Empty;
        public int? SerialNumberId { get; set; }
        public TypeNamebase? SerialNo { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
        public int? WarrantyMonths { get; set; }
        public DateTimeOffset? WarrantyStartDate { get; set; }
        public DateTimeOffset? WarrantyEndDate { get; set; }
    }
}