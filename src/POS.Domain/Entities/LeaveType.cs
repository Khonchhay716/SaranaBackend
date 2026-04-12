using POS.Domain.Common;

namespace POS.Domain.Entities
{
    public class LeaveType : BaseAuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public int MaxDaysPerYear { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    }
}