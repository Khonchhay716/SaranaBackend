using POS.Domain.Common;

namespace POS.Domain.Entities
{
    public class Branch : BaseAuditableEntity
    {
        public string  BranchName  { get; set; } = string.Empty;
        public string? Logo        { get; set; }
        public string  Status      { get; set; } = "Active";
        public string? Description { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}