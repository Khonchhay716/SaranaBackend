using POS.Domain.Common;
using POS.Domain.Entities;


namespace POS.Domain.Entities
{
    public class Customer : BaseAuditableEntity
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ImageProfile { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal TotalPoint { get; set; } = 0;
        public bool Status { get; set; } = true;
        public virtual Person? Person { get; set; }
    }
}