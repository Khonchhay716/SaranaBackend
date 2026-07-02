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

            // ---- 1) Try match a SERIAL NO first (serialized products) ----
            var serial = await _context.SerialStocks
                .AsNoTracking()
                .Include(s => s.Product)
                    .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(s => s.SerialNo == code && !s.Product.IsDeleted, cancellationToken);

            if (serial != null)
            {
                if (serial.Status != SerialStatus.Available)
                    return ApiResponse<ProductScanInfo>.NotFound($"Serial '{code}' is not available (status: {serial.Status}).");

                var serialResult = MapToScanInfo(serial.Product, isSerial: true, scannedSerial: serial.SerialNo);
                return ApiResponse<ProductScanInfo>.Ok(serialResult, "Serial number matched.");
            }

            // ---- 2) Try match a PRODUCT CODE (non-serialized products) ----
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Code == code && !p.IsDeleted, cancellationToken);

            if (product == null)
                return ApiResponse<ProductScanInfo>.NotFound($"Product not found with code or serial this '{code}'.");

            // Code belongs to a serialized product -> must scan the unit's serial, not the product code
            if (product.ProductType == ProductType.Serialized)
                return ApiResponse<ProductScanInfo>.NotFound($"'{product.Name}' is a serialized product. Please scan the item's serial number instead of the product code.");

            var availableQty = await _context.NonSerialStocks
                .AsNoTracking()
                .Where(ns => ns.ProductId == product.Id)
                .SumAsync(ns => (int?)ns.Quantity, cancellationToken) ?? 0;

            if (availableQty <= 0)
                return ApiResponse<ProductScanInfo>.NotFound($"'{product.Name}' is out of stock.");

            var result = MapToScanInfo(product, isSerial: false, scannedSerial: null, quantityOverride: availableQty);
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