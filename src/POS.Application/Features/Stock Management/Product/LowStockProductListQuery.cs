using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;

namespace POS.Application.Features.StockManagement.Products
{
    public class LowStockProductListQuery : PaginationRequest, IRequest<PaginatedResult<ProductInfo>>
    {
        public string? Search { get; set; }
        public ProductType? ProductType { get; set; }
        public int? CategoryId { get; set; }
    }

    public class LowStockProductListQueryHandler : IRequestHandler<LowStockProductListQuery, PaginatedResult<ProductInfo>>
    {
        private readonly IMyAppDbContext _context;
        public LowStockProductListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<ProductInfo>> Handle(LowStockProductListQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Products
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                // only products with alert enabled (threshold > 0) and stock at/below threshold
                .Where(x => x.LowStockThreshold > 0 && x.StockQuantity <= x.LowStockThreshold);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(x =>
                    x.Name.Contains(request.Search) ||
                    (x.Code ?? "").Contains(request.Search));
            }

            if (request.ProductType.HasValue)
            {
                query = query.Where(x => x.ProductType == request.ProductType.Value);
            }

            if (request.CategoryId.HasValue)
            {
                query = query.Where(x => x.CategoryId == request.CategoryId.Value);
            }

            // most critical (lowest stock relative to threshold, then lowest stock) first
            query = query.OrderBy(x => x.StockQuantity).ThenByDescending(x => x.Id);

            var result = query.Select(x => new ProductInfo
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                ProductType = x.ProductType.ToString(),
                Unit = x.Unit,
                CostPrice = x.CostPrice,
                SalePrice = x.SalePrice,
                StockQuantity = x.StockQuantity,
                ImageUrl = x.ImageUrl,
                LowStockThreshold = x.LowStockThreshold,
                CreatedDate = x.CreatedDate,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Name : null
            });

            return await result.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}