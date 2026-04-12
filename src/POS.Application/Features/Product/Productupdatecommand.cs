using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;

namespace POS.Application.Features.Product
{
    public record ProductUpdateCommand : IRequest<ApiResponse<ProductInfo>>
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public decimal Price { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal TaxRate { get; set; } = 0;   // ✅ Added (0–100)
        public int? CategoryId { get; set; }
        public string? ImageProduct { get; set; }
        public bool IsSerialNumber { get; set; }
        public int MinStock { get; set; } = 0;
        public int? BranchId { get; set; }
        public string? RAM { get; set; }
        public string? Storage { get; set; }
    }

    public class ProductUpdateCommandValidator : AbstractValidator<ProductUpdateCommand>
    {
        public ProductUpdateCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MinStock).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TaxRate)                                             // ✅ Validate TaxRate
                .InclusiveBetween(0, 100).WithMessage("TaxRate must be between 0 and 100.");
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
            // 1. Fetch product
            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (product == null)
                return ApiResponse<ProductInfo>.NotFound($"Product {request.Id} not found");

            // 2. Validate
            var validator = new ProductUpdateCommandValidator();
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return ApiResponse<ProductInfo>.BadRequest(errors);
            }

            // 3. ✅ Check Barcode uniqueness (exclude current product)
            if (!string.IsNullOrWhiteSpace(request.Barcode))
            {
                var barcodeExists = await _context.Products
                    .AnyAsync(p => p.Barcode == request.Barcode
                                   && p.Id != request.Id
                                   && !p.IsDeleted, cancellationToken);
                if (barcodeExists)
                    return ApiResponse<ProductInfo>.BadRequest($"Barcode '{request.Barcode}' is already in use.");
            }

            // 4. Validate Category
            if (request.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories
                    .AnyAsync(c => c.Id == request.CategoryId && !c.IsDeleted, cancellationToken);
                if (!categoryExists)
                    return ApiResponse<ProductInfo>.BadRequest("Invalid Category");
            }

            // 5. Validate Branch
            if (request.BranchId.HasValue)
            {
                var branchExists = await _context.Branches
                    .AnyAsync(b => b.Id == request.BranchId.Value && !b.IsDeleted, cancellationToken);
                if (!branchExists)
                    return ApiResponse<ProductInfo>.BadRequest("Invalid Branch");
            }

            // 6. Update fields
            product.Name           = request.Name;
            product.Description    = request.Description;
            product.SKU            = request.SKU;
            product.Barcode        = request.Barcode;
            product.Price          = request.Price;
            product.CostPrice      = request.CostPrice;
            product.TaxRate        = request.TaxRate;       // ✅ Mapped
            product.CategoryId     = request.CategoryId;
            product.ImageProduct   = request.ImageProduct;
            product.IsSerialNumber = request.IsSerialNumber;
            product.MinStock       = request.MinStock;
            product.BranchId       = request.BranchId;
            product.RAM            = request.RAM;
            product.Storage        = request.Storage;
            product.UpdatedDate    = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // 7. Reload with navigations
            var updated = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Branch)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            var productInfo = updated!.Adapt<ProductInfo>();

            if (updated.Category != null)
                productInfo.Category = new TypeNamebase { Id = updated.Category.Id, Name = updated.Category.Name };
            if (updated.Branch != null)
                productInfo.Branch = new TypeNamebase { Id = updated.Branch.Id, Name = updated.Branch.BranchName };

            productInfo.SerialNumbers  = new List<SerialNumberInfo>();
            productInfo.StockMovements = new List<StockMovementInfo>();

            return ApiResponse<ProductInfo>.Ok(productInfo, "Product updated successfully");
        }
    }
}