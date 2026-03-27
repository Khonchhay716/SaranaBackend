using POS.Application.Common.Typebase;
using POS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace POS.Application.Features.Order
{
    public class OrderInfo
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
        
        public OrderStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public SaleType SaleType { get; set; }
        public string SaleTypeName { get; set; } = string.Empty;
        public PaymentStatus PaymentStatus { get; set; }
        public string PaymentStatusName { get; set; } = string.Empty;
        public PaymentMethodCode? PaymentMethod { get; set; }
        public string? PaymentMethodName { get; set; }
        
        public string? Notes { get; set; }
        
        public DateTimeOffset CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        
        public List<OrderItemInfo> OrderItems { get; set; } = new List<OrderItemInfo>();
    }
    
    public class OrderItemInfo
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSKU { get; set; }
        
        public int? SerialNumberId { get; set; }
        public string? SerialNo { get; set; }
        
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
        
        public string? Notes { get; set; }
    }
}