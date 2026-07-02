using POS.Domain.Common;

namespace POS.Domain.Entities.StockManagement
{
    public class NonSerialStock : BaseEntity
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        // Navigation
        public Product Product { get; set; } = default!;
    }
}