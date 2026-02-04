// POS.Application/Features/LibraryMember/LibraryMemberInfo.cs
using POS.Application.Common.Typebase;

namespace POS.Application.Features.LibraryMember
{
    public class LibraryMemberInfo
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public LibraryMemberStatus Status { get; set; }
        public TypeNamebase Person { get; set; }
        public string PersonName { get; set; } = string.Empty;
        public string MembershipNo { get; set; } = string.Empty;
        public int? MembershipType { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int MaxBooksAllowed { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public int? ApproveBy { get; set; }
    }
}