using POS.Application.Common.Typebase;

namespace POS.Application.Features.Product
{
    public class ProductInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public decimal Price { get; set; }
        public decimal? CostPrice { get; set; }
        public int Stock { get; set; }
        public string? ImageProduct { get; set; }
        public string? RAM { get; set; }
        public string? Storage { get; set; }
        public int? CategoryId { get; set; }
        public TypeNamebase? Category { get; set; }
        public int? BranchId { get; set; }
        public TypeNamebase? Branch { get; set; }
        public bool IsSerialNumber { get; set; }
        public int MinStock { get; set; } = 0;
        public bool IsLowStock => MinStock > 0 && Stock <= MinStock;
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTimeOffset? DeletedDate { get; set; }
        public string? DeletedBy { get; set; }
        public List<SerialNumberInfo> SerialNumbers { get; set; } = new();
        public List<StockMovementInfo> StockMovements { get; set; } = new();
    }

    public class ProductInfoForSale
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public decimal Price { get; set; }
        public decimal? CostPrice { get; set; }
        public int Stock { get; set; }
        public string? ImageProduct { get; set; }
        public string? RAM { get; set; }
        public string? Storage { get; set; }
        public TypeNamebase? Category { get; set; }
        public TypeNamebase? Branch { get; set; }
    }

    public class SerialNumberInfo
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string SerialNo { get; set; } = string.Empty;
        public string? Status { get; set; }
        public decimal Price { get; set; }
        public decimal? CostPrice { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
    }

    public class StockMovementInfo
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Type { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal? CostPrice { get; set; }
        public string? Notes { get; set; }
        public DateTimeOffset MovementDate { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
    }
}
