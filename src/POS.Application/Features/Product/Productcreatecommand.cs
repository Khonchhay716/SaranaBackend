using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using DomainProduct = POS.Domain.Entities.Product;

namespace POS.Application.Features.Product
{
    public record ProductCreateCommand : IRequest<ApiResponse<ProductInfo>>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public decimal Price { get; set; }
        public decimal? CostPrice { get; set; }
        public int? CategoryId { get; set; }
        public string? ImageProduct { get; set; }
        public bool IsSerialNumber { get; set; } = false;
        public int MinStock { get; set; } = 0;
        public int? BranchId { get; set; }
        public string? RAM { get; set; }
        public string? Storage { get; set; }
    }

    public class ProductCreateCommandValidator : AbstractValidator<ProductCreateCommand>
    {
        public ProductCreateCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MinStock).GreaterThanOrEqualTo(0);
        }
    }

    public class ProductCreateCommandHandler : IRequestHandler<ProductCreateCommand, ApiResponse<ProductInfo>>
    {
        private readonly IMyAppDbContext _context;

        public ProductCreateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<ProductInfo>> Handle(ProductCreateCommand request, CancellationToken cancellationToken)
        {
            var validator = new ProductCreateCommandValidator();
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return ApiResponse<ProductInfo>.BadRequest(errors);
            }

            if (request.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories
                    .AnyAsync(c => c.Id == request.CategoryId.Value && !c.IsDeleted, cancellationToken);
                if (!categoryExists)
                    return ApiResponse<ProductInfo>.NotFound($"Category with id {request.CategoryId.Value} not found");
            }

            // Validate Branch
            if (request.BranchId.HasValue)
            {
                var branchExists = await _context.Branches
                    .AnyAsync(b => b.Id == request.BranchId.Value && !b.IsDeleted, cancellationToken);
                if (!branchExists)
                    return ApiResponse<ProductInfo>.NotFound($"Branch with id {request.BranchId.Value} not found");
            }

            var product = new DomainProduct
            {
                Name = request.Name,
                Description = request.Description,
                SKU = request.SKU,
                Barcode = request.Barcode,
                Price = request.Price,
                CostPrice = request.CostPrice,
                CategoryId = request.CategoryId,
                ImageProduct = request.ImageProduct,
                Stock = 0,
                IsSerialNumber = request.IsSerialNumber,
                MinStock = request.MinStock,
                BranchId = request.BranchId,
                RAM = request.RAM,
                Storage = request.Storage,
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync(cancellationToken);

            // Reload with navigation props
            var created = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Branch)
                .AsNoTracking()
                .FirstAsync(p => p.Id == product.Id, cancellationToken);

            var data = created.Adapt<ProductInfo>();

            if (created.Category != null)
                data.Category = new TypeNamebase { Id = created.Category.Id, Name = created.Category.Name };

            if (created.Branch != null)
                data.Branch = new TypeNamebase { Id = created.Branch.Id, Name = created.Branch.BranchName };

            return ApiResponse<ProductInfo>.Created(data, "Product created successfully");
        }
    }
}
