using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;

namespace POS.Application.Features.StockManagement.Products
{
    // ==================== QUERY ====================
    // ✅ Stock-out time scan: staff scans the physical unit's SERIAL NO (not the product code)
    // when pulling a serialized product from stock to hand out to the customer.
    public record SerialScanQuery : IRequest<ApiResponse<ProductScanInfo>>
    {
        public string SerialNo { get; set; } = default!;
    }

    public class SerialScanQueryValidator : AbstractValidator<SerialScanQuery>
    {
        public SerialScanQueryValidator()
        {
            RuleFor(x => x.SerialNo)
                .NotEmpty().WithMessage("Scanned serial number is required.")
                .MaximumLength(100);
        }
    }

    // ==================== HANDLER ====================
    public class SerialScanQueryHandler : IRequestHandler<SerialScanQuery, ApiResponse<ProductScanInfo>>
    {
        private readonly IMyAppDbContext _context;
        public SerialScanQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<ProductScanInfo>> Handle(SerialScanQuery request, CancellationToken cancellationToken)
        {
            var code = request.SerialNo.Trim();

            var serial = await _context.SerialStocks
                .AsNoTracking()
                .Include(s => s.Product)
                    .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(s => s.SerialNo == code && !s.Product.IsDeleted, cancellationToken);

            if (serial == null)
                return ApiResponse<ProductScanInfo>.NotFound($"Serial '{code}' not found.");

            if (serial.Status != SerialStatus.Available)
                return ApiResponse<ProductScanInfo>.NotFound($"Serial '{code}' is not available (status: {serial.Status}).");

            var result = new ProductScanInfo
            {
                ProductId = serial.Product.Id,
                ProductCode = serial.Product.Code,
                ProductName = serial.Product.Name,
                ImageUrl = serial.Product.ImageUrl,
                ProductType = serial.Product.ProductType.ToString(),
                Unit = serial.Product.Unit,
                SalePrice = serial.Product.SalePrice,
                StockQuantity = serial.Product.StockQuantity,
                IsSerial = true,
                ScannedSerialNumber = serial.SerialNo,
                CategoryId = serial.Product.CategoryId,
                CategoryName = serial.Product.Category != null ? serial.Product.Category.Name : null
            };

            return ApiResponse<ProductScanInfo>.Ok(result, "Serial number matched.");
        }
    }
}
