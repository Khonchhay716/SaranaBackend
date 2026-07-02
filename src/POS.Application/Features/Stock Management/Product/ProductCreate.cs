using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities.StockManagement;
using POS.Domain.Enums;

namespace POS.Application.Features.StockManagement.Products
{
    public record ProductCreateCommand : IRequest<ApiResponse<ProductInfo>>
    {
        public string? Code { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public ProductType ProductType { get; set; }
        public string? Unit { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int LowStockThreshold { get; set; }
        public int? CategoryId { get; set; }
    }

    public class ProductCreateCommandValidator : AbstractValidator<ProductCreateCommand>
    {
        private readonly IMyAppDbContext _context;
        public ProductCreateCommandValidator(IMyAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(200)
                .MustAsync(async (name, ct) =>
                    !await _context.Products.AnyAsync(x => x.Name == name && !x.IsDeleted, ct))
                .WithMessage("Product name already exists.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Code is required for NonSerialized product.")
                .MaximumLength(50)
                .MustAsync(async (command, code, ct) =>
                    !await _context.Products.AnyAsync(x => x.Code == code, ct))
                .WithMessage("Product code already exists.");
            RuleFor(x => x.CategoryId)
                .MustAsync(async (categoryId, ct) =>
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

    public class ProductCreateCommandHandler : IRequestHandler<ProductCreateCommand, ApiResponse<ProductInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public ProductCreateCommandHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<ProductInfo>> Handle(ProductCreateCommand request, CancellationToken cancellationToken)
        {
            var product = new Product
            {
                Code = request.Code?.Trim(),
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                ImageUrl = request.ImageUrl?.Trim(),
                ProductType = request.ProductType,
                Unit = request.Unit?.Trim(),
                CostPrice = request.CostPrice,
                SalePrice = request.SalePrice,
                StockQuantity = 0,
                LowStockThreshold = request.LowStockThreshold,
                CategoryId = request.CategoryId,
                IsDeleted = false,
                CreatedDate = DateTimeOffset.UtcNow,
                CreatedBy = _currentUserService.UserId
            };

            _context.Products.Add(product);
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

            return ApiResponse<ProductInfo>.Ok(res!, "Product created successfully.");
        }
    }
}