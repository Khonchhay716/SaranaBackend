using MediatR;
using POS.Application.Common.Dto;

namespace POS.Application.Features.Product
{
    // Response DTOs
    public class ProductInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public DateTimeOffset UpdatedDate { get; set; }
    }

    // Request DTOs
    public record ProductCreateCommand : IRequest<ApiResponse<ProductInfo>>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
    }

    public record ProductUpdateCommand : IRequest<ApiResponse<ProductInfo>>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public record ProductDeleteCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
    }

    public class ProductQuery : IRequest<ApiResponse<ProductInfo>>
    {
        public int Id { get; set; }
    }

    public class ProductListQuery : PaginationRequest, IRequest<PaginatedResult<ProductInfo>>
    {
        public string? Search { get; set; }
        public string? Category { get; set; }
        public string? Brand { get; set; }
        public bool? IsActive { get; set; }
        public bool? LowStockOnly { get; set; }
    }
}