using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;

namespace POS.Application.Features.StockManagement.StockMovements
{
    public class CurrentStockListQuery : PaginationRequest, IRequest<PaginatedResult<CurrentStockInfo>>
    {
        public int? ProductId { get; set; }
        public string? SearchTerm { get; set; }
        public ProductType? ProductType { get; set; }
    }

    public class CurrentStockListQueryHandler : IRequestHandler<CurrentStockListQuery, PaginatedResult<CurrentStockInfo>>
    {
        private readonly IMyAppDbContext _context;

        public CurrentStockListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<CurrentStockInfo>> Handle(CurrentStockListQuery request, CancellationToken cancellationToken)
        {
            // ទាញយកស្តុកប្រភេទមិនមាន Serial (NonSerialized)
            var nonSerialQuery = _context.NonSerialStocks
                .AsNoTracking()
                .Include(x => x.Product)
                .Where(x => !x.IsDeleted && !x.Product.IsDeleted)
                .Select(x => new CurrentStockInfo
                {
                    ProductId = x.ProductId,
                    ProductCode = x.Product.Code,
                    ProductName = x.Product.Name,
                    ImageUrl = x.Product.ImageUrl,
                    ProductType = x.Product.ProductType == ProductType.Serialized ? "Serial Number" : "Non-Serial Number",
                    AvailableQuantity = x.Quantity,
                    TotalProductQuantity = x.Product.StockQuantity
                });

            // ទាញយកស្តុកប្រភេទមាន Serial (Serialized)
            var serialQuery = _context.SerialStocks
                .AsNoTracking()
                .Include(x => x.Product)
                .Where(x => !x.IsDeleted && !x.Product.IsDeleted && x.Status == SerialStatus.Available)
                .GroupBy(x => new 
                { 
                    x.ProductId, 
                    ProductCode = x.Product.Code,
                    ProductName = x.Product.Name,
                    ImageUrl = x.Product.ImageUrl, 
                    TotalProductQuantity = x.Product.StockQuantity, 
                    ProductType = x.Product.ProductType 
                })
                .Select(x => new CurrentStockInfo
                {
                    ProductId = x.Key.ProductId,
                    ProductCode = x.Key.ProductCode,
                    ProductName = x.Key.ProductName,
                    ImageUrl = x.Key.ImageUrl,
                    ProductType = x.Key.ProductType == ProductType.Serialized ? "Serial Number" : "Non-Serial Number",
                    AvailableQuantity = x.Count(),
                    TotalProductQuantity = x.Key.TotalProductQuantity
                });

            // បញ្ចូលគ្នា
            var combinedQuery = nonSerialQuery.Concat(serialQuery);
            if (request.ProductId.HasValue)
                combinedQuery = combinedQuery.Where(x => x.ProductId == request.ProductId.Value);

            if (request.ProductType.HasValue)
            {
                var typeString = request.ProductType.Value == ProductType.Serialized 
                    ? "Serial Number" 
                    : "Non-Serial Number";
                combinedQuery = combinedQuery.Where(x => x.ProductType == typeString);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                combinedQuery = combinedQuery.Where(x => 
                    x.ProductName.ToLower().Contains(term) || 
                    (x.ProductCode != null && x.ProductCode.ToLower().Contains(term)));
            }

            var result = combinedQuery.OrderBy(x => x.ProductName);

            return await result.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}