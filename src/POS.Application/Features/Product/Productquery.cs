using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
 
namespace POS.Application.Features.Product
{
    public class ProductQuery : IRequest<ApiResponse<ProductInfo>>
    {
        public int Id { get; set; }
    }
 
    public class ProductQueryHandler : IRequestHandler<ProductQuery, ApiResponse<ProductInfo>>
    {
        private readonly IMyAppDbContext _context;
 
        public ProductQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }
 
        public async Task<ApiResponse<ProductInfo>> Handle(ProductQuery request, CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Branch)
                // ✅ No SerialNumbers — use GET /product/{id}/serial-numbers
                // ✅ No StockMovements — use GET /product/{id}/stock-movements
                .AsNoTracking()
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == request.Id, cancellationToken);
 
            if (product == null)
                return ApiResponse<ProductInfo>.NotFound($"Product with id {request.Id} was not found");
 
            var info = new ProductInfo
            {
                Id             = product.Id,
                Name           = product.Name,
                Description    = product.Description,
                SKU            = product.SKU,
                Barcode        = product.Barcode,
                Price          = product.Price,
                CostPrice      = product.CostPrice,
                Stock          = product.Stock,
                ImageProduct   = product.ImageProduct,
                RAM            = product.RAM,
                Storage        = product.Storage,
                CategoryId     = product.CategoryId,
                IsSerialNumber = product.IsSerialNumber,
                MinStock       = product.MinStock,
                BranchId       = product.BranchId,
                Category = product.Category != null ? new TypeNamebase
                {
                    Id   = product.Category.Id,
                    Name = product.Category.Name,
                } : null,
                Branch = product.Branch != null ? new TypeNamebase
                {
                    Id   = product.Branch.Id,
                    Name = product.Branch.BranchName,
                } : null,
                IsDeleted   = product.IsDeleted,
                CreatedDate = product.CreatedDate,
                CreatedBy   = product.CreatedBy,
                UpdatedDate = product.UpdatedDate,
                UpdatedBy   = product.UpdatedBy,
                DeletedDate = product.DeletedDate,
                DeletedBy   = product.DeletedBy,
                SerialNumbers  = new List<SerialNumberInfo>(),
                StockMovements = new List<StockMovementInfo>(),
            };
 
            return ApiResponse<ProductInfo>.Ok(info, "Product retrieved successfully");
        }
    }
}