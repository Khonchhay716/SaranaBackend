// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Interfaces;
// using POS.Domain.Enums;

// namespace POS.Application.Features.Order
// {
//     // ============================================================
//     // QUERY
//     // ============================================================
//     public class DataListInDashboardQuery : IRequest<DataListInDashboardResponse>
//     {
//         public int? StaffId { get; set; }
//         public int? CustomerId { get; set; }
//         public DateTimeOffset? FromDate { get; set; }
//         public DateTimeOffset? ToDate { get; set; }
//     }

//     // ============================================================
//     // RESPONSE
//     // ============================================================
//     public class DataListInDashboardResponse
//     {
//         public decimal TotalSaleAmount { get; set; }
//         public decimal TotalCashSaleAmount { get; set; }
//         public decimal TotalPointSaleAmount { get; set; }
//         public decimal TotalDiscountAmount { get; set; }
//         public decimal TotalTaxAmount { get; set; }

//         public decimal TotalCashReceived { get; set; }

//         public int TotalEarnedPoints { get; set; }
//         public int TotalPointsUsed { get; set; }

//         public int TotalOrders { get; set; }
//         public int TotalCancelledOrders { get; set; }
//         public int TotalPendingOrders { get; set; }
//         public int TotalCompletedOrders { get; set; }
//         public int TotalRefundedOrders { get; set; }
//     }

//     // ============================================================
//     // HANDLER
//     // ============================================================
//     public class DataListInDashboardQueryHandler : IRequestHandler<DataListInDashboardQuery, DataListInDashboardResponse>
//     {
//         private readonly IMyAppDbContext _context;

//         public DataListInDashboardQueryHandler(IMyAppDbContext context)
//         {
//             _context = context;
//         }

//         public async Task<DataListInDashboardResponse> Handle(
//             DataListInDashboardQuery request,
//             CancellationToken cancellationToken)
//         {
//             // ==================== Base Query ====================
//             var query = _context.Orders
//                 .Where(o => !o.IsDeleted)
//                 .AsNoTracking();

//             // ==================== Filters ====================
//             if (request.StaffId.HasValue)
//                 query = query.Where(o => o.StaffId == request.StaffId.Value);

//             if (request.CustomerId.HasValue)
//                 query = query.Where(o => o.CustomerId == request.CustomerId.Value);

//             if (request.FromDate.HasValue)
//             {
//                 var fromDate = request.FromDate.Value.UtcDateTime.Date;
//                 query = query.Where(o => o.OrderDate >= fromDate);
//             }

//             if (request.ToDate.HasValue)
//             {
//                 var toDate = request.ToDate.Value.UtcDateTime.Date.AddDays(1);
//                 query = query.Where(o => o.OrderDate < toDate);
//             }

//             // ==================== Aggregate (Single Pass 🔥) ====================
//             var result = await query
//                 .GroupBy(o => 1)
//                 .Select(g => new DataListInDashboardResponse
//                 {
//                     // ==================== Sale ====================
//                     TotalSaleAmount = g.Sum(o =>
//                         (o.Status != OrderStatus.Cancelled &&
//                          o.PaymentStatus != PaymentStatus.Refunded)
//                         ? o.TotalAmount : 0),

//                     TotalCashSaleAmount = g.Sum(o =>
//                         (o.Status != OrderStatus.Cancelled &&
//                          o.PaymentStatus != PaymentStatus.Refunded &&
//                          o.PaymentMethod != PaymentMethodCode.Point)
//                         ? o.TotalAmount : 0),

//                     TotalPointSaleAmount = g.Sum(o =>
//                         (o.Status != OrderStatus.Cancelled &&
//                          o.PaymentStatus != PaymentStatus.Refunded &&
//                          o.PaymentMethod == PaymentMethodCode.Point)
//                         ? o.TotalAmount : 0),

//                     TotalDiscountAmount = g.Sum(o =>
//                         (o.Status != OrderStatus.Cancelled &&
//                          o.PaymentStatus != PaymentStatus.Refunded)
//                         ? o.DiscountAmount : 0),

//                     TotalTaxAmount = g.Sum(o =>
//                         (o.Status != OrderStatus.Cancelled &&
//                          o.PaymentStatus != PaymentStatus.Refunded)
//                         ? (o.TaxAmount ?? 0) : 0),

//                     // ==================== Cash ====================
//                     TotalCashReceived = g.Sum(o =>
//                         (o.Status != OrderStatus.Cancelled &&
//                          o.PaymentStatus != PaymentStatus.Refunded)
//                         ? o.CashReceived : 0),

//                     // ==================== Point ====================
//                     TotalEarnedPoints = g.Sum(o =>
//                         (o.Status != OrderStatus.Cancelled &&
//                          o.PaymentStatus != PaymentStatus.Refunded)
//                         ? o.EarnedPoints : 0),

//                     TotalPointsUsed = g.Sum(o =>
//                         (o.Status != OrderStatus.Cancelled &&
//                          o.PaymentStatus != PaymentStatus.Refunded)
//                         ? o.PointsUsed : 0),

//                     // ==================== Orders ====================
//                     TotalOrders = g.Count(),

//                     TotalCancelledOrders = g.Count(o =>
//                         o.Status == OrderStatus.Cancelled),

//                     TotalPendingOrders = g.Count(o =>
//                         o.Status == OrderStatus.Pending),

//                     TotalCompletedOrders = g.Count(o =>
//                         o.Status == OrderStatus.Completed &&
//                         o.PaymentStatus != PaymentStatus.Refunded),

//                     TotalRefundedOrders = g.Count(o =>
//                         o.PaymentStatus == PaymentStatus.Refunded),
//                 })
//                 .FirstOrDefaultAsync(cancellationToken)
//                 ?? new DataListInDashboardResponse();

//             return result;
//         }
//     }
// }


using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;

namespace POS.Application.Features.Order
{
    // ============================================================
    // QUERY
    // ============================================================
    public class DataListInDashboardQuery : IRequest<DataListInDashboardResponse>
    {
        public int? StaffId { get; set; }
        public int? CustomerId { get; set; }
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
    }

    // ============================================================
    // RESPONSE
    // ============================================================
    public class DataListInDashboardResponse
    {
        // ==================== Sale ====================
        public decimal TotalSaleAmount { get; set; }
        public decimal TotalCashSaleAmount { get; set; }
        public decimal TotalPointSaleAmount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal TotalTaxAmount { get; set; }

        // ==================== Cash ====================
        public decimal TotalCashReceived { get; set; }

        // ==================== Point ====================
        public int TotalEarnedPoints { get; set; }
        public int TotalPointsUsed { get; set; }

        // ==================== Orders ====================
        public int TotalOrders { get; set; }
        public int TotalCancelledOrders { get; set; }
        public int TotalPendingOrders { get; set; }
        public int TotalCompletedOrders { get; set; }
        public int TotalRefundedOrders { get; set; }

        // ==================== Master Data ====================
        public int TotalStaffs { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalBranches { get; set; }
        public int TotalCategories { get; set; }
    }

    // ============================================================
    // HANDLER
    // ============================================================
    public class DataListInDashboardQueryHandler : IRequestHandler<DataListInDashboardQuery, DataListInDashboardResponse>
    {
        private readonly IMyAppDbContext _context;

        public DataListInDashboardQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<DataListInDashboardResponse> Handle(
            DataListInDashboardQuery request,
            CancellationToken cancellationToken)
        {
            // ==================== Base Query ====================
            var query = _context.Orders
                .Where(o => !o.IsDeleted)
                .AsNoTracking();

            // ==================== Filters (Orders only) ====================
            if (request.StaffId.HasValue)
                query = query.Where(o => o.StaffId == request.StaffId.Value);

            if (request.CustomerId.HasValue)
                query = query.Where(o => o.CustomerId == request.CustomerId.Value);

            if (request.FromDate.HasValue)
            {
                var fromDate = request.FromDate.Value.UtcDateTime.Date;
                query = query.Where(o => o.OrderDate >= fromDate);
            }

            if (request.ToDate.HasValue)
            {
                var toDate = request.ToDate.Value.UtcDateTime.Date.AddDays(1);
                query = query.Where(o => o.OrderDate < toDate);
            }

            // ==================== Orders Aggregate ====================
            // ✅ await ម្តងៗ — EF Core DbContext មិន thread-safe
            var result = await query
                .GroupBy(o => 1)
                .Select(g => new DataListInDashboardResponse
                {
                    // ==================== Sale ====================
                    TotalSaleAmount = g.Sum(o =>
                        (o.Status != OrderStatus.Cancelled &&
                         o.PaymentStatus != PaymentStatus.Refunded)
                        ? o.TotalAmount : 0),

                    TotalCashSaleAmount = g.Sum(o =>
                        (o.Status != OrderStatus.Cancelled &&
                         o.PaymentStatus != PaymentStatus.Refunded &&
                         o.PaymentMethod != PaymentMethodCode.Point)
                        ? o.TotalAmount : 0),

                    TotalPointSaleAmount = g.Sum(o =>
                        (o.Status != OrderStatus.Cancelled &&
                         o.PaymentStatus != PaymentStatus.Refunded &&
                         o.PaymentMethod == PaymentMethodCode.Point)
                        ? o.TotalAmount : 0),

                    TotalDiscountAmount = g.Sum(o =>
                        (o.Status != OrderStatus.Cancelled &&
                         o.PaymentStatus != PaymentStatus.Refunded)
                        ? o.DiscountAmount : 0),

                    TotalTaxAmount = g.Sum(o =>
                        (o.Status != OrderStatus.Cancelled &&
                         o.PaymentStatus != PaymentStatus.Refunded)
                        ? (o.TaxAmount ?? 0) : 0),

                    // ==================== Cash ====================
                    TotalCashReceived = g.Sum(o =>
                        (o.Status != OrderStatus.Cancelled &&
                         o.PaymentStatus != PaymentStatus.Refunded)
                        ? o.CashReceived : 0),

                    // ==================== Point ====================
                    TotalEarnedPoints = g.Sum(o =>
                        (o.Status != OrderStatus.Cancelled &&
                         o.PaymentStatus != PaymentStatus.Refunded)
                        ? o.EarnedPoints : 0),

                    TotalPointsUsed = g.Sum(o =>
                        (o.Status != OrderStatus.Cancelled &&
                         o.PaymentStatus != PaymentStatus.Refunded)
                        ? o.PointsUsed : 0),

                    // ==================== Orders ====================
                    TotalOrders = g.Count(),

                    TotalCancelledOrders = g.Count(o =>
                        o.Status == OrderStatus.Cancelled),

                    TotalPendingOrders = g.Count(o =>
                        o.Status == OrderStatus.Pending),

                    TotalCompletedOrders = g.Count(o =>
                        o.Status == OrderStatus.Completed &&
                        o.PaymentStatus != PaymentStatus.Refunded),

                    TotalRefundedOrders = g.Count(o =>
                        o.PaymentStatus == PaymentStatus.Refunded),
                })
                .FirstOrDefaultAsync(cancellationToken)
                ?? new DataListInDashboardResponse();

            // ==================== Master Data ====================
            // ✅ await ម្តងៗ — fix DbContext threading error
            // ✅ queries ទាំងនេះលឿនណាស់ (COUNT លើ small table)
            result.TotalStaffs     = await _context.Staffs.CountAsync(s => !s.IsDeleted, cancellationToken);
            result.TotalCustomers  = await _context.Customers.CountAsync(c => !c.IsDeleted, cancellationToken);
            result.TotalBranches   = await _context.Branches.CountAsync(b => !b.IsDeleted, cancellationToken);
            result.TotalCategories = await _context.Categories.CountAsync(c => !c.IsDeleted, cancellationToken);

            return result;
        }
    }
}