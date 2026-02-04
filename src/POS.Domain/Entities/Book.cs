// POS.Domain/Entities/Book.cs - Add CategoryId and Navigation
using POS.Domain.Common;

namespace POS.Domain.Entities
{
    public class Book : BaseAuditableEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? ISBN { get; set; }
        public string? Publisher { get; set; }
        public string? Edition { get; set; }
        public DateTimeOffset PublishedYear { get; set; }
        public int TotalQty { get; set; }
        public int AvailableQty { get; set; }
        public decimal Price { get; set; }
        public string RackNo { get; set; } = string.Empty;
        public string No { get; set; } = string.Empty;
        
        // Add Category
        public int? CategoryId { get; set; }
        public virtual Category? Category { get; set; }
    }
}