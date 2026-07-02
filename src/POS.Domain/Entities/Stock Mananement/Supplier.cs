using POS.Domain.Common;

namespace POS.Domain.Entities.StockManagement
{
    public class Supplier : BaseEntity
    {
        public string Name { get; set; } = default!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    }
}