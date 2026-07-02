using POS.Domain.Enums;

namespace POS.Application.Features.StockManagement.StockMovements
{
    public class CurrentStockInfo
    {
        public int ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string ProductName { get; set; } = default!;
        public string? ImageUrl { get; set; }
        public string? ProductType { get; set; }
        public int AvailableQuantity { get; set; } 
        public int TotalProductQuantity { get; set; }
    }
}