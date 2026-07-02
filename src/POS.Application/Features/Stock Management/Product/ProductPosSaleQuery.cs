using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;

namespace POS.Application.Features.StockManagement.Products
{
    public class ProductPosSaleQuery : PaginationRequest, IRequest<PaginatedResult<ProductPosSaleInfo>>
    {
        public string? Search { get; set; }
        public ProductType? ProductType { get; set; }
        public int? CategoryId { get; set; }
    }

    public class ProductPosSaleQueryHandler : IRequestHandler<ProductPosSaleQuery, PaginatedResult<ProductPosSaleInfo>>
    {
        private readonly IMyAppDbContext _context;

        public ProductPosSaleQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<ProductPosSaleInfo>> Handle(ProductPosSaleQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Products
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (request.ProductType.HasValue)
            {
                query = query.Where(x => x.ProductType == request.ProductType.Value);
            }

            if (request.CategoryId.HasValue)
            {
                query = query.Where(x => x.CategoryId == request.CategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(x =>
                    (x.Code ?? "").ToLower().Contains(search) ||
                    x.Name.ToLower().Contains(search));
            }

            query = query
                .OrderByDescending(x => x.StockQuantity > 0)
                .ThenBy(x => x.Name);

            var result = query.Select(x => new ProductPosSaleInfo
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                ImageUrl = x.ImageUrl,
                Unit = x.Unit,
                SalePrice = x.SalePrice,
                StockQuantity = x.StockQuantity,
                ProductType = x.ProductType.ToString(),
                InStock = x.StockQuantity > 0,
                Description = x.Description,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Name : null
            });

            return await result.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}