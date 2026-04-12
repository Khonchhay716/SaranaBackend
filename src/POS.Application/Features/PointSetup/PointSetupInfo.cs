namespace POS.Application.Features.PointSetup
{
    public class PointSetupInfo
    {
        public int Id { get; set; }
        public decimal PointValue { get; set; }
        public decimal MinOrderAmount { get; set; }
        public int? MaxPointPerOrder { get; set; }
        public decimal PointsPerRedemption { get; set; }
        public bool IsActive { get; set; }

        public DateTimeOffset CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }
    }
}