using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;

namespace POS.Application.Features.StockManagement.Products
{
    // ==================== QUERY ====================
    public record ProductScanQuery : IRequest<ApiResponse<ProductScanInfo>>
    {
        public string Code { get; set; } = default!; // scanned barcode: could be a SerialNo or a Product Code
    }

    public class ProductScanQueryValidator : AbstractValidator<ProductScanQuery>
    {
        public ProductScanQueryValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Scanned code is required.")
                .MaximumLength(100);
        }
    }

    // ==================== HANDLER ====================
    public class ProductScanQueryHandler : IRequestHandler<ProductScanQuery, ApiResponse<ProductScanInfo>>
    {
        private readonly IMyAppDbContext _context;
        public ProductScanQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<ProductScanInfo>> Handle(ProductScanQuery request, CancellationToken cancellationToken)
        {
            var code = request.Code.Trim();

            // ✅ Sale-time scan: always by PRODUCT CODE, for both serialized and non-serialized
            // products. The individual unit serial is no longer picked here - it's scanned later,
            // at stock-out time, via SerialScanQuery.
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Code == code && !p.IsDeleted, cancellationToken);

            if (product == null)
                return ApiResponse<ProductScanInfo>.NotFound($"Product not found with code '{code}'.");

            int availableQty;
            if (product.ProductType == ProductType.Serialized)
            {
                availableQty = await _context.SerialStocks
                    .AsNoTracking()
                    .CountAsync(s => s.ProductId == product.Id && !s.IsDeleted && s.Status == SerialStatus.Available, cancellationToken);
            }
            else
            {
                availableQty = await _context.NonSerialStocks
                    .AsNoTracking()
                    .Where(ns => ns.ProductId == product.Id)
                    .SumAsync(ns => (int?)ns.Quantity, cancellationToken) ?? 0;
            }

            if (availableQty <= 0)
                return ApiResponse<ProductScanInfo>.NotFound($"'{product.Name}' is out of stock.");

            var result = MapToScanInfo(product, isSerial: product.ProductType == ProductType.Serialized, scannedSerial: null, quantityOverride: availableQty);
            return ApiResponse<ProductScanInfo>.Ok(result, "Product matched by code.");
        }

        private static ProductScanInfo MapToScanInfo(
            POS.Domain.Entities.StockManagement.Product product,
            bool isSerial,
            string? scannedSerial,
            int? quantityOverride = null)
        {
            return new ProductScanInfo
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
                ImageUrl = product.ImageUrl,
                ProductType = product.ProductType.ToString(),
                Unit = product.Unit,
                SalePrice = product.SalePrice,
                StockQuantity = quantityOverride ?? product.StockQuantity,
                IsSerial = isSerial,
                ScannedSerialNumber = scannedSerial,
                CategoryId = product.CategoryId,
                CategoryName = product.Category != null ? product.Category.Name : null
            };
        }
    }
}