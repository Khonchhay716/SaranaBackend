using POS.Domain.Common;
namespace POS.Domain.Entities
{
    public class Product : BaseAuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public decimal Price { get; set; }
        public decimal? CostPrice { get; set; }
         public decimal TaxRate { get; set; } = 0;
        public int Stock { get; set; }
        public string? ImageProduct { get; set; }
        public bool IsSerialNumber { get; set; } = false;
        public int MinStock { get; set; }
        public string? RAM { get; set; }
        public string? Storage { get; set; }
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public ICollection<SerialNumber> SerialNumbers { get; set; } = new List<SerialNumber>();
        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
         public ICollection<ProductDiscount> ProductDiscounts { get; set; } = new List<ProductDiscount>();
    }

    public class SerialNumber : BaseAuditableEntity
    {
        public int ProductId { get; set; }
        public string SerialNo { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public DateTimeOffset? SoldDate { get; set; }
        public OrderItem? OrderItem { get; set; }
        public Product Product { get; set; } = null!;
    }

    public class StockMovement : BaseAuditableEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public string Type { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public string? Notes { get; set; }
        public DateTimeOffset MovementDate { get; set; } = DateTimeOffset.UtcNow;
    }
}