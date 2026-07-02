// POS.Application/Features/Dashboard/DashboardSummaryQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Features.Orders;
using POS.Application.Features.StockManagement.StockMovements;

namespace POS.Application.Features.Dashboard
{
    public record DashboardSummaryQuery : IRequest<ApiResponse<DashboardSummaryInfo>>
    {
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
    }

    // Lightweight stock summary — only the fields the dashboard needs
    public class DashboardStockSummaryInfo
    {
        public decimal GrandTotalPrice { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalSerialPrice { get; set; }
        public int TotalSerialQty { get; set; }
        public decimal TotalNonSerialPrice { get; set; }
        public int TotalNonSerialQty { get; set; }
    }

    public class DashboardSummaryInfo
    {
        public OrderSalesSummaryInfo SalesSummary { get; set; } = new();
        public DashboardStockSummaryInfo StockSummary { get; set; } = new();

        public int TotalSuppliers { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalStaff { get; set; }
        public int TotalCategories { get; set; }
    }

    public class DashboardSummaryQueryHandler : IRequestHandler<DashboardSummaryQuery, ApiResponse<DashboardSummaryInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly IMediator _mediator;

        public DashboardSummaryQueryHandler(IMyAppDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public async Task<ApiResponse<DashboardSummaryInfo>> Handle(DashboardSummaryQuery request, CancellationToken cancellationToken)
        {
            // ===== Sales Summary (reuse existing handler) =====
            var salesResult = await _mediator.Send(new OrderSalesSummaryQuery
            {
                StartDate = request.FromDate,
                EndDate = request.ToDate
            }, cancellationToken);

            // ===== Stock In Summary (reuse existing handler, take only needed fields) =====
            var stockResult = await _mediator.Send(new StockInSummaryQuery
            {
                From = request.FromDate,
                To = request.ToDate
            }, cancellationToken);

            var stockSummary = new DashboardStockSummaryInfo
            {
                GrandTotalPrice = stockResult.GrandTotalPrice,
                TotalQuantity = stockResult.TotalQuantity,
                TotalSerialPrice = stockResult.TotalSerialPrice,
                TotalSerialQty = stockResult.TotalSerialQty,
                TotalNonSerialPrice = stockResult.TotalNonSerialPrice,
                TotalNonSerialQty = stockResult.TotalNonSerialQty,
            };

            // ===== Counts =====
            // ⚠️ DbContext is NOT thread-safe — must run sequentially, never Task.WhenAll on the same context.
            var totalSuppliers = await _context.Suppliers.AsNoTracking().CountAsync(x => !x.IsDeleted, cancellationToken);
            var totalCustomers = await _context.Customers.AsNoTracking().CountAsync(x => !x.IsDeleted, cancellationToken);
            var totalStaff = await _context.Staffs.AsNoTracking().CountAsync(x => !x.IsDeleted, cancellationToken);
            var totalCategories = await _context.Categories.AsNoTracking().CountAsync(x => !x.IsDeleted, cancellationToken);

            var result = new DashboardSummaryInfo
            {
                SalesSummary = salesResult.Data ?? new OrderSalesSummaryInfo(),
                StockSummary = stockSummary,
                TotalSuppliers = totalSuppliers,
                TotalCustomers = totalCustomers,
                TotalStaff = totalStaff,
                TotalCategories = totalCategories,
            };

            return ApiResponse<DashboardSummaryInfo>.Ok(result, "Dashboard summary retrieved successfully.");
        }
    }
}