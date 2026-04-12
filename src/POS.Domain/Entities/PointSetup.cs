using POS.Domain.Common;

namespace POS.Domain.Entities
{
    public class PointSetup : BaseAuditableEntity
    {
        public decimal PointValue { get; set; } = 0;
        public decimal MinOrderAmount { get; set; } = 0;
        public int? MaxPointPerOrder { get; set; } = null;
        public decimal PointsPerRedemption { get; set; } = 0;
        public bool IsActive { get; set; } = false;
    }
}