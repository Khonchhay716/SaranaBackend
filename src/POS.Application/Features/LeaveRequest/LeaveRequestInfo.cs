namespace POS.Application.Features.Leave
{
    public class LeaveRequestInfo
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string StaffImage { get; set; } = string.Empty;
        public int LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalDays { get; set; }      // ✅ decimal
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Session { get; set; } = "FullDay"; // ✅ add
        public int? ApproverId { get; set; }
        public string? ApproverName { get; set; }
        public DateTimeOffset? ApprovedDate { get; set; }
        public string? ApprovalNote { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTimeOffset? DeletedDate { get; set; }
        public string? DeletedBy { get; set; }
    }
}