using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;

namespace POS.Application.Features.Order
{
    public class DataListInDashboardQuery : IRequest<DataListInDashboardResponse>
    {
        public int?            StaffId    { get; set; }
        public int?            CustomerId { get; set; }
        public DateTimeOffset? FromDate   { get; set; }
        public DateTimeOffset? ToDate     { get; set; }
    }

    public class DataListInDashboardResponse
    {
        // ==================== Sale ====================
        public decimal TotalSaleAmount      { get; set; }
        public decimal TotalCashSaleAmount  { get; set; }
        public decimal TotalPointSaleAmount { get; set; }
        public decimal TotalDiscountAmount  { get; set; }
        public decimal TotalTaxAmount       { get; set; }

        // ==================== Cash ====================
        public decimal TotalCashReceived    { get; set; }

        // ==================== Point ====================
        public int TotalEarnedPoints        { get; set; }
        public int TotalPointsUsed          { get; set; }

        // ==================== Orders ====================
        public int TotalOrders              { get; set; }
        public int TotalCancelledOrders     { get; set; }
        public int TotalPendingOrders       { get; set; }
        public int TotalCompletedOrders     { get; set; }
        public int TotalRefundedOrders      { get; set; }  // ✅ add
    }

    public class DataListInDashboardQueryHandler : IRequestHandler<DataListInDashboardQuery, DataListInDashboardResponse>
    {
        private readonly IMyAppDbContext _context;

        public DataListInDashboardQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<DataListInDashboardResponse> Handle(
            DataListInDashboardQuery request,
            CancellationToken        cancellationToken)
        {
            var query = _context.Orders
                .Where(o => !o.IsDeleted)
                .AsNoTracking();

            // ==================== Filters ====================
            if (request.StaffId.HasValue)
                query = query.Where(o => o.StaffId == request.StaffId.Value);

            if (request.CustomerId.HasValue)
                query = query.Where(o => o.CustomerId == request.CustomerId.Value);

            if (request.FromDate.HasValue)
                query = query.Where(o => o.OrderDate >= request.FromDate.Value.Date);

            if (request.ToDate.HasValue)
                query = query.Where(o => o.OrderDate <= request.ToDate.Value.Date.AddDays(1).AddTicks(-1));

            // ==================== Aggregate ====================
            var result = await query
                .GroupBy(o => 1)
                .Select(g => new DataListInDashboardResponse
                {
                    // ==================== Sale ====================
                    // ✅ exclude Cancelled + Refunded
                    TotalSaleAmount = g
                        .Where(o => o.Status        != OrderStatus.Cancelled
                                 && o.PaymentStatus != PaymentStatus.Refunded)
                        .Sum(o => o.TotalAmount),

                    // ✅ Cash + QR only, exclude Cancelled + Refunded
                    TotalCashSaleAmount = g
                        .Where(o => o.Status        != OrderStatus.Cancelled
                                 && o.PaymentStatus != PaymentStatus.Refunded
                                 && o.PaymentMethod != PaymentMethodCode.Point)
                        .Sum(o => o.TotalAmount),

                    // ✅ Point only, exclude Cancelled + Refunded
                    TotalPointSaleAmount = g
                        .Where(o => o.Status        != OrderStatus.Cancelled
                                 && o.PaymentStatus != PaymentStatus.Refunded
                                 && o.PaymentMethod == PaymentMethodCode.Point)
                        .Sum(o => o.TotalAmount),

                    // ✅ exclude Cancelled + Refunded
                    TotalDiscountAmount = g
                        .Where(o => o.Status        != OrderStatus.Cancelled
                                 && o.PaymentStatus != PaymentStatus.Refunded)
                        .Sum(o => o.DiscountAmount),

                    // ✅ exclude Cancelled + Refunded
                    TotalTaxAmount = g
                        .Where(o => o.Status        != OrderStatus.Cancelled
                                 && o.PaymentStatus != PaymentStatus.Refunded)
                        .Sum(o => o.TaxAmount ?? 0),

                    // ==================== Cash ====================
                    // ✅ exclude Cancelled + Refunded
                    TotalCashReceived = g
                        .Where(o => o.Status        != OrderStatus.Cancelled
                                 && o.PaymentStatus != PaymentStatus.Refunded)
                        .Sum(o => o.CashReceived),

                    // ==================== Point ====================
                    // ✅ exclude Cancelled + Refunded
                    TotalEarnedPoints = g
                        .Where(o => o.Status        != OrderStatus.Cancelled
                                 && o.PaymentStatus != PaymentStatus.Refunded)
                        .Sum(o => o.EarnedPoints),

                    // ✅ exclude Cancelled + Refunded
                    TotalPointsUsed = g
                        .Where(o => o.Status        != OrderStatus.Cancelled
                                 && o.PaymentStatus != PaymentStatus.Refunded)
                        .Sum(o => o.PointsUsed),

                    // ==================== Orders ====================
                    TotalOrders = g.Count(),

                    TotalCancelledOrders = g
                        .Count(o => o.Status == OrderStatus.Cancelled),

                    TotalPendingOrders = g
                        .Count(o => o.Status == OrderStatus.Pending),

                    TotalCompletedOrders = g
                        .Count(o => o.Status        == OrderStatus.Completed
                                 && o.PaymentStatus != PaymentStatus.Refunded),  // ✅

                    // ✅ add refunded count
                    TotalRefundedOrders = g
                        .Count(o => o.PaymentStatus == PaymentStatus.Refunded),
                })
                .FirstOrDefaultAsync(cancellationToken) ?? new DataListInDashboardResponse();

            return result;
        }
    }
}