// StockReturnInfo.cs
using POS.Application.Common.Typebase;
using POS.Domain.Enums;

namespace POS.Application.Features.StockManagement.StockReturns
{
    public class StockReturnInfo
    {
        public int Id { get; set; }
        public string ReturnNo { get; set; } = default!;
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = default!;
        public string? Note { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Status { get; set; } 
        public DateTimeOffset CreatedDate { get; set; }
        public TypeNamebase? CreatedBy { get; set; }
        public List<StockReturnItemInfo> Items { get; set; } = new();
    }

    public class StockReturnItemInfo
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = default!;
        public string? ProductCode { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; } 
        public string Reason { get; set; } = default!;
        public string? Note { get; set; }
        public List<string>? SerialNumbers { get; set; }
    }
}