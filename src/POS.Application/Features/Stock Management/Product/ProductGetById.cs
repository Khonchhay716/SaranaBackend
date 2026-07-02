using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.StockManagement.Products
{
    public record ProductGetByIdQuery : IRequest<ApiResponse<ProductInfo>>
    {
        public int Id { get; set; }
    }

    public class ProductGetByIdQueryHandler : IRequestHandler<ProductGetByIdQuery, ApiResponse<ProductInfo>>
    {
        private readonly IMyAppDbContext _context;
        public ProductGetByIdQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<ProductInfo>> Handle(ProductGetByIdQuery request, CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Where(x => x.Id == request.Id && !x.IsDeleted)
                .Select(x => new ProductInfo
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Description = x.Description,
                    ProductType = x.ProductType.ToString(),
                    Unit = x.Unit,
                    CostPrice = x.CostPrice,
                    SalePrice = x.SalePrice,
                    ImageUrl = x.ImageUrl,
                    StockQuantity = x.StockQuantity,
                    LowStockThreshold = x.LowStockThreshold,
                    CreatedDate = x.CreatedDate,
                    CategoryId = x.CategoryId,
                    CategoryName = x.Category != null ? x.Category.Name : null
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (product == null)
                return ApiResponse<ProductInfo>.NotFound($"Product with Id {request.Id} was not found.");

            return ApiResponse<ProductInfo>.Ok(product);
        }
    }
}