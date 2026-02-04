// POS.Application/Features/BookIssue/BookIssueInfo.cs
namespace POS.Application.Features.BookIssue
{
    public class BookIssueInfo
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string BookAuthor { get; set; } = string.Empty;
        public int LibraryMemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MembershipNo { get; set; } = string.Empty;
        public DateTimeOffset IssueDate { get; set; }
        public DateTimeOffset DueDate { get; set; }
        public DateTimeOffset? ReturnDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int DaysOverdue => ReturnDate == null && DateTimeOffset.UtcNow > DueDate 
            ? (int)(DateTimeOffset.UtcNow - DueDate).TotalDays 
            : 0;
        public string? Notes { get; set; }
    }
}