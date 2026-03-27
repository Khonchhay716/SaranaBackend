// POS.Application/Features/Product/ProductStockSummaryQuery.cs

using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Product
{
    public class ProductStockSummaryQuery : IRequest<ApiResponse<ProductStockSummaryInfo>>
    {
        public int Id { get; set; }
    }

    public class ProductStockSummaryInfo
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public bool IsSerialNumber { get; set; }
        public int Sold { get; set; }
        public int TotalBatchesOut { get; set; }
        public int TotalBatchesIn { get; set; }
        public int CurrentStock { get; set; }
        public int TotalStock { get; set; }
        public int TotalStockIn { get; set; }
        public int TotalStockOut { get; set; }
        public decimal TotalCostIn { get; set; }
        public decimal TotalCostOut { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class ProductStockSummaryQueryHandler
        : IRequestHandler<ProductStockSummaryQuery, ApiResponse<ProductStockSummaryInfo>>
    {
        private readonly IMyAppDbContext _context;

        public ProductStockSummaryQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<ProductStockSummaryInfo>> Handle(
            ProductStockSummaryQuery request,
            CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.Id && !p.IsDeleted, cancellationToken);

            if (product == null)
                return ApiResponse<ProductStockSummaryInfo>.NotFound($"Product {request.Id} not found");

            var info = new ProductStockSummaryInfo
            {
                ProductId = product.Id,
                ProductName = product.Name,
                IsSerialNumber = product.IsSerialNumber,
            };

            if (product.IsSerialNumber)
            {
                var snStats = await _context.SerialNumbers
                    .Where(s => s.ProductId == request.Id && !s.IsDeleted)
                    .GroupBy(s => 1)
                    .Select(g => new
                    {
                        TotalStock = g.Count(),
                        CurrentStock = g.Count(x => x.Status == "Available"),
                        Sold = g.Count(x => x.Status == "Sold"),
                        TotalCost = g.Sum(x => x.CostPrice)
                    })
                    .FirstOrDefaultAsync(cancellationToken);
                info.CurrentStock = snStats?.CurrentStock ?? 0;
                info.TotalStock = snStats?.TotalStock ?? 0;
                info.Sold = snStats?.Sold ?? 0;
                info.TotalCostIn = snStats?.TotalCost ?? 0;
                info.TotalCostOut = 0;
                info.TotalCost = info.TotalCostIn;
            }
            else
            {
                var smStats = await _context.StockMovements
                    .Where(sm => sm.ProductId == request.Id && !sm.IsDeleted)
                    .GroupBy(sm => 1)
                    .Select(g => new
                    {
                        TotalBatchesOut = g.Count(x => x.Type != "StockIn"),
                        TotalBatchesIn = g.Count(x => x.Type == "StockIn"),

                        TotalStockIn = g.Where(x => x.Type == "StockIn")
                                        .Sum(x => (int?)x.Quantity) ?? 0,

                        TotalStockOut = g.Where(x => x.Type == "StockOut")
                                         .Sum(x => (int?)x.Quantity) ?? 0,

                        TotalCostIn = g.Where(x => x.Type == "StockIn")
                                       .Sum(x => (decimal?)(x.CostPrice * x.Quantity)) ?? 0,

                        TotalCostOut = g.Where(x => x.Type == "StockOut")
                                        .Sum(x => (decimal?)(x.CostPrice * x.Quantity)) ?? 0
                    })
                    .FirstOrDefaultAsync(cancellationToken);
                info.TotalBatchesOut = smStats?.TotalBatchesOut ?? 0;
                info.TotalBatchesIn = smStats?.TotalBatchesIn ?? 0;
                info.TotalStockIn = smStats?.TotalStockIn ?? 0;
                info.TotalStockOut = smStats?.TotalStockOut ?? 0;
                info.CurrentStock = product.Stock;
                info.TotalStock = smStats?.TotalStockIn - smStats?.TotalStockOut ?? 0;
                info.Sold = smStats?.TotalStockIn - smStats?.TotalStockOut - product.Stock ?? 0;
                info.TotalCostIn = smStats?.TotalCostIn ?? 0;
                info.TotalCostOut = smStats?.TotalCostOut ?? 0;
                info.TotalCost = info.TotalCostIn - info.TotalCostOut;
            }

            return ApiResponse<ProductStockSummaryInfo>
                .Ok(info, "Summary retrieved successfully");
        }
    }
}