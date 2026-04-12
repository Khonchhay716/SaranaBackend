using POS.Domain.Common;

namespace POS.Domain.Entities
{
    public class LeaveRequest : BaseAuditableEntity
    {
        public int StaffId { get; set; }
        public virtual Staff Staff { get; set; } = null!;
        public int LeaveTypeId { get; set; }
        public virtual LeaveType LeaveType { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalDays { get; set; }      // ✅ decimal for 0.5
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string Session { get; set; } = "FullDay"; // ✅ add

        public int? ApproverId { get; set; }
        public virtual Staff? Approver { get; set; }
        public DateTimeOffset? ApprovedDate { get; set; }
        public string? ApprovalNote { get; set; }
    }
}