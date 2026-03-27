using POS.Domain.Common;
 
namespace POS.Domain.Entities
{
    public class Category : BaseAuditableEntity
    {
        public string  Name        { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Image       { get; set; }
        public bool    IsActive    { get; set; } = true;
 
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
 