namespace POS.Application.Features.StockManagement.Suppliers
{
    public class SupplierInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
    }

    public class SupplierLookup
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
    }
}