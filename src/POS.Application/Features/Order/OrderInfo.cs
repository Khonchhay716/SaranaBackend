// POS.Application/Features/Orders/OrderInfo.cs
using POS.Application.Common.Typebase;

namespace POS.Application.Features.Orders
{
    public class OrderInfo
    {
        public int Id { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = "Walk-in Customer";
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PointEarned { get; set; }
        public decimal PointUsed { get; set; }
        public string? Note { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public TypeNamebase? CreateBy {get; set;}
        public List<OrderItemInfo> Items { get; set; } = new();
    }

    public class OrderItemInfo
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? ImageUrl { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? DiscountName { get; set; }
        public decimal GlobalDiscountAmount { get; set; }
        public string? GlobalDiscountName { get; set; }
        public decimal LineTotal { get; set; }
        public List<string>? SerialNumbers { get; set; }
        public DateTimeOffset? FulfilledDate { get; set; }

        // ✅ Warranty - calculated from start/end dates
        public int? WarrantyDays { get; set; }
        public DateTimeOffset? WarrantyStartDate { get; set; }
        public DateTimeOffset? WarrantyEndDate { get; set; }
        public bool HasWarranty => WarrantyDays.HasValue && WarrantyDays.Value > 0;
        public bool IsWarrantyActive => WarrantyEndDate.HasValue && WarrantyEndDate.Value > DateTimeOffset.UtcNow;
        public int? RemainingWarrantyDays
        {
            get
            {
                if (!WarrantyEndDate.HasValue) return null;
                var remaining = (WarrantyEndDate.Value - DateTimeOffset.UtcNow).Days;
                return remaining > 0 ? remaining : 0;
            }
        }
        public string WarrantyStatus
        {
            get
            {
                if (!HasWarranty) return "No Warranty";
                if (!WarrantyStartDate.HasValue) return "Not Started";
                if (IsWarrantyActive) return "Active";
                return "Expired";
            }
        }
    }

    public class OrderSummaryInfo
    {
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = "Walk-in Customer";
        public decimal? CustomerAvailablePoint { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PointEarned { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<OrderItemInfo> Items { get; set; } = new();
    }
}