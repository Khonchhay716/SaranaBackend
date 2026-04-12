using POS.Domain.Common;

namespace POS.Domain.Entities
{
    public class Staff : BaseAuditableEntity
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ImageProfile { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public bool Status { get; set; } = true;
        public int? SupervisorId { get; set; }
        public Staff? Supervisor { get; set; } 

        public virtual Person? Person { get; set; }
    }
}