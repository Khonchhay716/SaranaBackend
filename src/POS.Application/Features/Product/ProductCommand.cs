using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using DomainProduct = POS.Domain.Entities.Product;

namespace POS.Application.Features.Product
{
    public class ProductCreateCommandHandler : IRequestHandler<ProductCreateCommand, ApiResponse<ProductInfo>>
    {
        private readonly IMyAppDbContext _context;

        public ProductCreateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<ProductInfo>> Handle(ProductCreateCommand request, CancellationToken cancellationToken)
        {
            // Check if SKU already exists
            var existingSku = await _context.Products
                .FirstOrDefaultAsync(x => x.SKU == request.SKU && !x.IsDeleted, cancellationToken);
                
            if (existingSku != null)
            {
                return ApiResponse<ProductInfo>.BadRequest("SKU already exists");
            }

            // Check if Barcode already exists
            if (!string.IsNullOrEmpty(request.Barcode))
            {
                var existingBarcode = await _context.Products
                    .FirstOrDefaultAsync(x => x.Barcode == request.Barcode && !x.IsDeleted, cancellationToken);
                    
                if (existingBarcode != null)
                {
                    return ApiResponse<ProductInfo>.BadRequest("Barcode already exists");
                }
            }

            var product = request.Adapt<DomainProduct>();
            product.CreatedDate = DateTimeOffset.UtcNow;
            product.UpdatedDate = DateTimeOffset.UtcNow;
            product.IsActive = true;

            _context.Products.Add(product);
            await _context.SaveChangesAsync(cancellationToken);

            var productInfo = product.Adapt<ProductInfo>();
            return ApiResponse<ProductInfo>.Created(productInfo, "Product created successfully");
        }
    }

    public class ProductUpdateCommandHandler : IRequestHandler<ProductUpdateCommand, ApiResponse<ProductInfo>>
    {
        private readonly IMyAppDbContext _context;

        public ProductUpdateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<ProductInfo>> Handle(ProductUpdateCommand request, CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (product == null)
            {
                return ApiResponse<ProductInfo>.NotFound($"Product with id {request.Id} was not found");
            }

            // Check if SKU is taken by another product
            var skuExists = await _context.Products
                .AnyAsync(x => x.SKU == request.SKU && x.Id != request.Id && !x.IsDeleted, cancellationToken);
                
            if (skuExists)
            {
                return ApiResponse<ProductInfo>.BadRequest("SKU already taken by another product");
            }

            // Check if Barcode is taken by another product
            if (!string.IsNullOrEmpty(request.Barcode))
            {
                var barcodeExists = await _context.Products
                    .AnyAsync(x => x.Barcode == request.Barcode && x.Id != request.Id && !x.IsDeleted, cancellationToken);
                    
                if (barcodeExists)
                {
                    return ApiResponse<ProductInfo>.BadRequest("Barcode already taken by another product");
                }
            }

            request.Adapt(product);
            product.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            var productInfo = product.Adapt<ProductInfo>();
            return ApiResponse<ProductInfo>.Ok(productInfo, $"Product with id {request.Id} was updated successfully");
        }
    }

    public class ProductDeleteCommandHandler : IRequestHandler<ProductDeleteCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;

        public ProductDeleteCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse> Handle(ProductDeleteCommand request, CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            
            if (product == null)
            {
                return ApiResponse.NotFound($"Product with id {request.Id} was not found");
            }

            product.IsDeleted = true;
            product.DeletedDate = DateTimeOffset.UtcNow;
            product.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Product with id {request.Id} deleted successfully");
        }
    }
}