using System.ComponentModel.DataAnnotations.Schema;
using POS.Domain.Enums.Coupon;

namespace POS.Domain.Entities
{
    public class Coupon
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int? Limit { get; set; }
        public decimal Discount { get; set; }
        public DiscountType Type { get; set; }
        public ConditionType ConditionType { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal ConditionValue { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool OncePerCustomer { get; set; }
        // public List<Product> Products { get; set; }
        public string Description { get; set; } = string.Empty;
        public Status Status { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedDate { get; set; }
        public DateTimeOffset DeletedDate { get; set; }
    }
}