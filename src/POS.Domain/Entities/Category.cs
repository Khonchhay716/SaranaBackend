// POS.Domain/Entities/Category.cs
using POS.Domain.Common;

namespace POS.Domain.Entities
{
    public class Category : BaseAuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Navigation Properties
        public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    }
}