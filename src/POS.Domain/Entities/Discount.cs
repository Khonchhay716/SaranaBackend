using POS.Domain.Common;
 
namespace POS.Domain.Entities
{
    public class Discount : BaseAuditableEntity
    {
        public string   Name            { get; set; } = string.Empty;
        public string?  Description     { get; set; }
        public string   Type            { get; set; } = "Percentage";
        public decimal  Value           { get; set; }
        public decimal? MinOrderAmount  { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate   { get; set; }
        public bool     IsActive        { get; set; } = true;
        public bool     IsAllProducts   { get; set; } = false;
 
        public ICollection<ProductDiscount> ProductDiscounts { get; set; } = new List<ProductDiscount>();
    }
 
    public class ProductDiscount : BaseAuditableEntity
    {
        public int      DiscountId { get; set; }
        public Discount Discount   { get; set; } = null!;
        public int      ProductId  { get; set; }
        public Product  Product    { get; set; } = null!;
    }
}
 