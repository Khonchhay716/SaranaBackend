// POS.Application/Features/Book/BookInfo.cs - Add Category
using POS.Application.Common.Typebase;

namespace POS.Application.Features.Book
{
    public class BookInfo
    {
        public int Id { get; set; }
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
        
        // Add Category Info
        public int? CategoryId { get; set; }
        public TypeNamebase? Category { get; set; }
    }
}