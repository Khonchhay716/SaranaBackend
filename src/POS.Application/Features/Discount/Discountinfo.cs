// POS.Application/Features/Discount/DiscountInfo.cs
namespace POS.Application.Features.Discount
{
    public class DiscountInfo
    {
        public int      Id              { get; set; }
        public string   Name            { get; set; } = string.Empty;
        public string?  Description     { get; set; }
        public string   Type            { get; set; } = "Percentage";
        public decimal  Value           { get; set; }
        public decimal? MinOrderAmount  { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate   { get; set; }
        public bool     IsActive        { get; set; }
        public bool IsGlobal { get; set; }
        public List<DiscountProductItem> Products { get; set; } = new();
        public bool             IsDeleted   { get; set; }
        public DateTimeOffset   CreatedDate { get; set; }
        public string?          CreatedBy   { get; set; }
        public DateTimeOffset?  UpdatedDate { get; set; }
        public string?          UpdatedBy   { get; set; }
    }

    public class DiscountProductItem
    {
        public int    ProductDiscountId { get; set; }
        public int    ProductId         { get; set; }
        public string ProductName       { get; set; } = string.Empty;
        public string? ProductSKU       { get; set; }
        public string? ImageProduct     { get; set; }
        public decimal Price            { get; set; }
    }
    public class DiscountInfoLookup
    {
        public int     Id    { get; set; }
        public string  Name  { get; set; } = string.Empty;
        public string  Type  { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
}