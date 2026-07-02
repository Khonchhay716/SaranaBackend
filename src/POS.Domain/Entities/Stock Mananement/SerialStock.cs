using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities.StockManagement
{
    public class SerialStock : BaseEntity
    {
        public int ProductId { get; set; }
        public string SerialNo { get; set; } = default!;
        public SerialStatus Status { get; set; }

        // Navigation
        public Product Product { get; set; } = default!;
    }
}