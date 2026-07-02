using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities.StockManagement
{
    public class Product : BaseEntity
    {
        public string? Code { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }
        public ProductType ProductType { get; set; }
        public string? Unit { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int LowStockThreshold { get; set; }

        // Foreign Keys
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
        public ICollection<SerialStock> SerialStocks { get; set; } = new List<SerialStock>();
        public ICollection<NonSerialStock> NonSerialStocks { get; set; } = new List<NonSerialStock>();
        public ICollection<StockAdjustment> StockAdjustments { get; set; } = new List<StockAdjustment>();
        public ICollection<ProductDiscount> ProductDiscounts { get; set; } = new List<ProductDiscount>();
    }
}