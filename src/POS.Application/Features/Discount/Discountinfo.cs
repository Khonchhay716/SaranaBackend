namespace POS.Application.Features.Discount
{
    public class DiscountInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = "Percentage";
        public decimal Value { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public bool IsActive { get; set; }
        public List<DiscountProductItem> Products { get; set; } = new();
        public bool IsDeleted { get; set; }
        public bool IsAllProducts { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }

    public class DiscountProductItem
    {
        public int ProductDiscountId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductCode { get; set; }
        public string? ImageUrl { get; set; }
        public decimal SalePrice { get; set; }
    }
    public class DiscountInfoLookup
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
}