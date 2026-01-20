using POS.Application.Common.Typebase;

namespace POS.Application.Features.Coupon
{
    public class CouponInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public TypeNamebase ConditionType { get; set; }
        public decimal ConditionValue { get; set; }
        public TypeNamebase Type { get; set; }
        public int? Limit { get; set; }
        public decimal Discount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool OncePerCustomer { get; set; }
        // public List<TypeNamebase> Products { get; set; }
        public string Description { get; set; } = string.Empty;
        public TypeNamebase Status { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }

    }
}


