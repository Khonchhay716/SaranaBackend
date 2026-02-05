// POS.Domain/Entities/BookIssue.cs
using POS.Domain.Common;

namespace POS.Domain.Entities
{
    public class BookIssue : BaseAuditableEntity
    {
        public int BookId { get; set; }
        public int LibraryMemberId { get; set; }
        public DateTimeOffset IssueDate { get; set; }
        public DateTimeOffset DueDate { get; set; }
        public DateTimeOffset? ReturnDate { get; set; }
        public string? Status { get; set; }
        public int? IssuedByPersonId { get; set; }
        public string? Notes { get; set; }
        public string? Allowed { get; set; } = "Pending";
        public int RenewCount { get; set; } = 0;
        public DateTimeOffset? LastRenewedDate { get; set; }

        // Navigation Properties
        public virtual Book Book { get; set; } = null!;
        public virtual LibraryMember LibraryMember { get; set; } = null!;
        public virtual Person? IssuedByPerson { get; set; }
    }
}