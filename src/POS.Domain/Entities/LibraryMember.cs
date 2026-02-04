// POS.Domain/Entities/LibraryMember.cs
using POS.Domain.Common;

namespace POS.Domain.Entities
{
    public class LibraryMember : BaseAuditableEntity
    {
        public int PersonId { get; set; }
        public string MembershipNo { get; set; } = string.Empty;
        public int MembershipType { get; set; }
        public string Email { get; set; } = string.Empty;
        public int Status { get; set; }
        public bool IsActive { get; set; } = true;
        public int MaxBooksAllowed { get; set; } = 5;
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public int? ApproveBy { get; set; }
        public virtual Person Person { get; set; } = null!;
        public virtual Person? ApprovedByUser { get; set; }
    }
}