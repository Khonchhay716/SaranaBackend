using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;

namespace POS.Application.Features.Product
{
    public class ProductSaleLookupQuery : PaginationRequest, IRequest<PaginatedResult<ProductInfoForSale>>
    {
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public int? BranchId { get; set; }
    }

    public class ProductSaleLookupQueryValidator : AbstractValidator<ProductSaleLookupQuery>
    {
        public ProductSaleLookupQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }
    public class ProductSaleLookupQueryHandler
        : IRequestHandler<ProductSaleLookupQuery, PaginatedResult<ProductInfoForSale>>
    {
        private readonly IMyAppDbContext _context;

        public ProductSaleLookupQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<ProductInfoForSale>> Handle(
            ProductSaleLookupQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Branch)
                .Include(p => p.SerialNumbers)
                .Where(p => !p.IsDeleted && p.Stock > 0)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(p =>
                    p.Name.Contains(request.Search) ||
                    (p.SKU != null && p.SKU.Contains(request.Search)) ||
                    (p.Barcode != null && p.Barcode.Contains(request.Search)));
            }

            if (request.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == request.CategoryId.Value);
            }
            if (request.BranchId.HasValue)
            {
                query = query.Where(p => p.BranchId == request.BranchId.Value);
            }

            query = query.OrderBy(p => p.Name);
            var resultQuery = query.Select(p => new ProductInfoForSale
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                SKU = p.SKU,
                Barcode = p.Barcode,
                Price = p.Price,
                CostPrice = p.CostPrice,
                TaxRate = p.TaxRate,
                Stock = p.Stock,
                IsSerialNumber = p.IsSerialNumber,
                ImageProduct = p.ImageProduct,
                RAM = p.RAM,
                Storage = p.Storage,
                Category = p.Category != null ? new TypeNamebase
                {
                    Id = p.Category.Id,
                    Name = p.Category.Name
                } : null,

                Branch = p.Branch != null ? new TypeNamebase
                {
                    Id = p.Branch.Id,
                    Name = p.Branch.BranchName
                } : null,

            });

            return await resultQuery.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}