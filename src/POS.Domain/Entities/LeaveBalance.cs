using POS.Domain.Common;

namespace POS.Domain.Entities
{
    public class LeaveBalance : BaseAuditableEntity
    {
        public int StaffId { get; set; }
        public virtual Staff Staff { get; set; } = null!;

        public int LeaveTypeId { get; set; }
        public virtual LeaveType LeaveType { get; set; } = null!;

        public int Year { get; set; }
        public decimal  TotalDays { get; set; }
        public decimal  UsedDays { get; set; }
        public decimal  RemainingDays => TotalDays - UsedDays;
    }
}