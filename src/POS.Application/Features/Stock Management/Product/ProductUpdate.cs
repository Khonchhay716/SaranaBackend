using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;
using System.Text.Json.Serialization;

namespace POS.Application.Features.StockManagement.Products
{
    public record ProductUpdateCommand : IRequest<ApiResponse<ProductInfo>>
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string? Code { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? Unit { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int LowStockThreshold { get; set; }
        public int? CategoryId { get; set; }
    }

    public class ProductUpdateCommandValidator : AbstractValidator<ProductUpdateCommand>
    {
        private readonly IMyAppDbContext _context;
        public ProductUpdateCommandValidator(IMyAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(200)
                .MustAsync(async (command, name, ct) =>
                    !await _context.Products.AnyAsync(x => x.Name == name && x.Id != command.Id && !x.IsDeleted, ct))
                .WithMessage("Product name already exists.");

            RuleFor(x => x.Code)
                .MaximumLength(50)
                .MustAsync(async (command, code, ct) =>
                    !await _context.Products.AnyAsync(x => x.Code == code && x.Id != command.Id && !x.IsDeleted, ct))
                .WithMessage("Product code already exists.")
                .When(x => !string.IsNullOrEmpty(x.Code));

            RuleFor(x => x.CategoryId)
                .MustAsync(async (command, categoryId, ct) =>
                    !categoryId.HasValue ||
                    await _context.Categories.AnyAsync(c => c.Id == categoryId.Value && !c.IsDeleted, ct))
                .WithMessage("Selected category does not exist.");

            RuleFor(x => x.CostPrice)
                .GreaterThan(0).WithMessage("Cost price must be greater than 0.");

            RuleFor(x => x.SalePrice)
                .GreaterThan(0).WithMessage("Sale price must be greater than 0.");

            RuleFor(x => x.LowStockThreshold)
                .GreaterThanOrEqualTo(0).WithMessage("Low stock threshold must be >= 0.");
        }
    }

    public class ProductUpdateCommandHandler : IRequestHandler<ProductUpdateCommand, ApiResponse<ProductInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public ProductUpdateCommandHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<ProductInfo>> Handle(ProductUpdateCommand request, CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (product == null)
                return ApiResponse<ProductInfo>.NotFound($"Product with Id {request.Id} was not found.");

            product.Code = request.Code?.Trim();
            product.Name = request.Name.Trim();
            product.Description = request.Description?.Trim();
            product.ImageUrl = request.ImageUrl?.Trim();
            product.Unit = request.Unit?.Trim();
            product.CostPrice = request.CostPrice;
            product.SalePrice = request.SalePrice;
            product.LowStockThreshold = request.LowStockThreshold;
            product.CategoryId = request.CategoryId;
            product.UpdatedDate = DateTimeOffset.UtcNow;
            product.UpdatedBy = _currentUserService.UserId;

            await _context.SaveChangesAsync(cancellationToken);

            var res = await _context.Products
                .AsNoTracking()
                .Where(x => x.Id == product.Id)
                .Select(x => new ProductInfo
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Description = x.Description,
                    ImageUrl = x.ImageUrl,
                    ProductType = x.ProductType.ToString(),
                    Unit = x.Unit,
                    CostPrice = x.CostPrice,
                    SalePrice = x.SalePrice,
                    StockQuantity = x.StockQuantity,
                    LowStockThreshold = x.LowStockThreshold,
                    CreatedDate = x.CreatedDate,
                    CategoryId = x.CategoryId,
                    CategoryName = x.Category != null ? x.Category.Name : null
                })
                .FirstOrDefaultAsync(cancellationToken);

            return ApiResponse<ProductInfo>.Ok(res!, "Product updated successfully.");
        }
    }
}