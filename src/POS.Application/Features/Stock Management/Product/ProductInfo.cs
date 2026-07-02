using POS.Domain.Enums;

namespace POS.Application.Features.StockManagement.Products
{
    public class ProductInfo
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? ProductType { get; set; }
        public string? Unit { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int StockQuantity { get; set; }
        public int LowStockThreshold { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
    }


    public class ProductPosSaleInfo
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? ImageUrl { get; set; }
        public string? Unit { get; set; }
        public string? ProductType { get; set; }
        public string? Description { get; set; }
        public decimal SalePrice { get; set; }
        public decimal StockQuantity { get; set; }
        public bool InStock { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
    }


    public class ProductScanInfo
    {
        public int ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string ProductName { get; set; } = default!;
        public string? ImageUrl { get; set; }
        public string ProductType { get; set; } = default!;
        public string? Unit { get; set; }
        public decimal SalePrice { get; set; }
        public int StockQuantity { get; set; }
        public bool IsSerial { get; set; }
        public string? ScannedSerialNumber { get; set; }
        public int? WarrantyDays { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
    }
}