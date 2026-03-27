using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Product
{
    public class ProductStockTotalSummaryQuery : IRequest<ApiResponse<ProductStockTotalSummaryInfo>>
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ProductStockTotalSummaryInfo
    {
        public int TotalStockSerial { get; set; }
        public int TotalStockMovement { get; set; }
        public int TotalStockAll { get; set; }
        public decimal TotalCostSerial { get; set; }
        public decimal TotalCostMovement { get; set; }
        public decimal TotalCostAll { get; set; }
    }

    public class ProductStockTotalSummaryQueryHandler
        : IRequestHandler<ProductStockTotalSummaryQuery, ApiResponse<ProductStockTotalSummaryInfo>>
    {
        private readonly IMyAppDbContext _context;

        public ProductStockTotalSummaryQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<ProductStockTotalSummaryInfo>> Handle(
            ProductStockTotalSummaryQuery request,
            CancellationToken cancellationToken)
        {
            var start = request.StartDate?.Date;
            var end = request.EndDate?.Date.AddDays(1).AddTicks(-1);

            var serialQuery = _context.SerialNumbers
                .Where(s => !s.IsDeleted);

            if (start.HasValue)
                serialQuery = serialQuery.Where(s => s.CreatedDate >= start.Value);

            if (end.HasValue)
                serialQuery = serialQuery.Where(s => s.CreatedDate <= end.Value);

            var serialStats = await serialQuery
                .GroupBy(x => 1)
                .Select(g => new
                {
                    TotalStock = g.Count(),
                    TotalCost = g.Sum(x => x.CostPrice)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var movementsQuery = _context.StockMovements
                .Where(sm => !sm.IsDeleted);

            if (start.HasValue)
                movementsQuery = movementsQuery.Where(sm => sm.CreatedDate >= start.Value);

            if (end.HasValue)
                movementsQuery = movementsQuery.Where(sm => sm.CreatedDate <= end.Value);

            var movementStats = await movementsQuery
                .GroupBy(sm => 1)
                .Select(g => new
                {
                    StockIn = g.Sum(x => x.Type == "StockIn" ? x.Quantity : 0),
                    StockOut = g.Sum(x => x.Type == "StockOut" ? x.Quantity : 0),
                    TotalCostIn = g.Sum(x => x.Type == "StockIn" ? x.CostPrice * x.Quantity : 0),
                    TotalCostOut = g.Sum(x => x.Type == "StockOut" ? x.CostPrice * x.Quantity : 0)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var info = new ProductStockTotalSummaryInfo
            {
                TotalStockSerial = serialStats?.TotalStock ?? 0,
                TotalCostSerial = serialStats?.TotalCost ?? 0,

                TotalStockMovement = (movementStats?.StockIn ?? 0) - (movementStats?.StockOut ?? 0),
                TotalCostMovement = (movementStats?.TotalCostIn ?? 0) - (movementStats?.TotalCostOut ?? 0),

                TotalStockAll = (serialStats?.TotalStock ?? 0)
                              + ((movementStats?.StockIn ?? 0) - (movementStats?.StockOut ?? 0)),
                TotalCostAll = (serialStats?.TotalCost ?? 0)
                              + ((movementStats?.TotalCostIn ?? 0) - (movementStats?.TotalCostOut ?? 0))
            };

            return ApiResponse<ProductStockTotalSummaryInfo>
                .Ok(info, "Total stock summary retrieved successfully");
        }
    }
}