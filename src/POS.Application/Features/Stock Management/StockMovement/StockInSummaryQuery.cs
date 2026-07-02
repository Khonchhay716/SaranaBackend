// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Interfaces;
// using POS.Domain.Enums;

// namespace POS.Application.Features.StockManagement.StockMovements
// {
//     public class StockInSummaryQuery : IRequest<StockInSummaryResult>
//     {
//         public int? ProductId { get; set; }
//         public int? SupplierId { get; set; }
//         public int? CreatedBy { get; set; }
//         public DateTimeOffset? From { get; set; }
//         public DateTimeOffset? To { get; set; }
//     }

//     public class StockInSummaryResult
//     {
//         public decimal GrandTotalPrice { get; set; }
//         public int TotalQuantity { get; set; }
//         public int StockInCount { get; set; }
//         public int StockOutCount { get; set; }
//         public int AdjustmentCount { get; set; }
//         public int ReturnOutCount { get; set; }
//         public int ReturnInCount { get; set; }
//     }

//     public class StockInSummaryQueryHandler : IRequestHandler<StockInSummaryQuery, StockInSummaryResult>
//     {
//         private readonly IMyAppDbContext _context;

//         public StockInSummaryQueryHandler(IMyAppDbContext context)
//         {
//             _context = context;
//         }

//         public async Task<StockInSummaryResult> Handle(StockInSummaryQuery request, CancellationToken cancellationToken)
//         {
//             var query = _context.StockMovements.AsNoTracking();

//             // Filters
//             if (request.ProductId.HasValue)
//                 query = query.Where(x => x.ProductId == request.ProductId.Value);

//             if (request.SupplierId.HasValue)
//                 query = query.Where(x => x.SupplierId == request.SupplierId.Value);

//             if (request.CreatedBy.HasValue)
//                 query = query.Where(x => x.CreatedBy == request.CreatedBy.Value);

//             if (request.From.HasValue)
//                 query = query.Where(x => x.CreatedDate >= request.From.Value);

//             if (request.To.HasValue)
//                 query = query.Where(x => x.CreatedDate < request.To.Value.AddDays(1));

//             var rawData = await query
//                 .GroupBy(x => 1)
//                 .Select(g => new
//                 {
//                     // Stock In
//                     StockInCount = g.Count(x => x.Type == MovementType.In),
//                     StockInQuantity = g.Where(x => x.Type == MovementType.In).Sum(x => x.Quantity),
//                     StockInTotalPrice = g.Where(x => x.Type == MovementType.In).Sum(x => x.TotalPrice),

//                     // Stock Out
//                     StockOutCount = g.Count(x => x.Type == MovementType.Out),

//                     PositiveAdjQuantity = g.Where(x => x.Type == MovementType.Adjustment && x.TypeAdjustment == TypeAdjustment.Over).Sum(x => x.Quantity),
//                     PositiveAdjTotalPrice = g.Where(x => x.Type == MovementType.Adjustment && x.TypeAdjustment == TypeAdjustment.Over)
//                                              .Sum(x => x.TotalPrice > 0 ? x.TotalPrice : (x.Quantity * x.UnitPrice)),

//                     NegativeAdjQuantity = g.Where(x => x.Type == MovementType.Adjustment && x.TypeAdjustment == TypeAdjustment.Lost).Sum(x => x.Quantity),
//                     NegativeAdjTotalPrice = g.Where(x => x.Type == MovementType.Adjustment && x.TypeAdjustment == TypeAdjustment.Lost)
//                                              .Sum(x => x.TotalPrice > 0 ? x.TotalPrice : (x.Quantity * x.UnitPrice)),

//                     // Total Adjustment Count
//                     AdjustmentCount = g.Count(x => x.Type == MovementType.Adjustment),
//                     ReturnOutCount = g.Count(x => x.Type == MovementType.ReturnOut),
//                     ReturnOutQuantity = g.Where(x => x.Type == MovementType.ReturnOut).Sum(x => x.Quantity),
//                     ReturnOutTotalPrice = g.Where(x => x.Type == MovementType.ReturnOut)
//                                            .Sum(x => x.TotalPrice > 0 ? x.TotalPrice : (x.Quantity * x.UnitPrice)),

//                     ReturnInCount = g.Count(x => x.Type == MovementType.ReturnIn),
//                     ReturnInQuantity = g.Where(x => x.Type == MovementType.ReturnIn).Sum(x => x.Quantity),
//                     ReturnInTotalPrice = g.Where(x => x.Type == MovementType.ReturnIn)
//                                           .Sum(x => x.TotalPrice > 0 ? x.TotalPrice : (x.Quantity * x.UnitPrice))
//                 })
//                 .FirstOrDefaultAsync(cancellationToken);

//             if (rawData == null)
//             {
//                 return new StockInSummaryResult();
//             }

//             return new StockInSummaryResult
//             {
//                 GrandTotalPrice = (rawData.StockInTotalPrice + rawData.PositiveAdjTotalPrice + rawData.ReturnInTotalPrice) 
//                                   - (rawData.NegativeAdjTotalPrice + rawData.ReturnOutTotalPrice),

//                 TotalQuantity = (rawData.StockInQuantity + rawData.PositiveAdjQuantity + rawData.ReturnInQuantity) 
//                                 - (rawData.NegativeAdjQuantity + rawData.ReturnOutQuantity),

//                 StockInCount = rawData.StockInCount,
//                 StockOutCount = rawData.StockOutCount,
//                 AdjustmentCount = rawData.AdjustmentCount,
//                 ReturnOutCount = rawData.ReturnOutCount,
//                 ReturnInCount = rawData.ReturnInCount 
//             };
//         }
//     }
// }


using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;

namespace POS.Application.Features.StockManagement.StockMovements
{
    public class StockInSummaryQuery : IRequest<StockInSummaryResult>
    {
        public int? ProductId { get; set; }
        public int? SupplierId { get; set; }
        public int? CreatedBy { get; set; }
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
    }

    public class StockInSummaryResult
    {
        public decimal GrandTotalPrice { get; set; }
        public int TotalQuantity { get; set; }
        public int StockInCount { get; set; }
        public int StockOutCount { get; set; }
        public int AdjustmentCount { get; set; }
        public int ReturnOutCount { get; set; }
        public int ReturnInCount { get; set; }

        // NEW: breakdown by product type
        public decimal TotalSerialPrice { get; set; }
        public int TotalSerialQty { get; set; }
        public decimal TotalNonSerialPrice { get; set; }
        public int TotalNonSerialQty { get; set; }
    }

    public class StockInSummaryQueryHandler : IRequestHandler<StockInSummaryQuery, StockInSummaryResult>
    {
        private readonly IMyAppDbContext _context;

        public StockInSummaryQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<StockInSummaryResult> Handle(StockInSummaryQuery request, CancellationToken cancellationToken)
        {
            var query = _context.StockMovements.AsNoTracking();

            // Filters
            if (request.ProductId.HasValue)
                query = query.Where(x => x.ProductId == request.ProductId.Value);

            if (request.SupplierId.HasValue)
                query = query.Where(x => x.SupplierId == request.SupplierId.Value);

            if (request.CreatedBy.HasValue)
                query = query.Where(x => x.CreatedBy == request.CreatedBy.Value);

            if (request.From.HasValue)
                query = query.Where(x => x.CreatedDate >= request.From.Value);

            if (request.To.HasValue)
                query = query.Where(x => x.CreatedDate < request.To.Value.AddDays(1));

            // join with Products to know ProductType (Serialized / NonSerialized)
            var joined = query.Join(
                _context.Products.AsNoTracking(),
                x => x.ProductId,
                p => p.Id,
                (x, p) => new { Movement = x, p.ProductType }
            );

            var rawData = await joined
                .GroupBy(x => 1)
                .Select(g => new
                {
                    // Stock In
                    StockInCount = g.Count(x => x.Movement.Type == MovementType.In),
                    StockInQuantity = g.Where(x => x.Movement.Type == MovementType.In).Sum(x => x.Movement.Quantity),
                    StockInTotalPrice = g.Where(x => x.Movement.Type == MovementType.In).Sum(x => x.Movement.TotalPrice),

                    // Stock Out
                    StockOutCount = g.Count(x => x.Movement.Type == MovementType.Out),

                    PositiveAdjQuantity = g.Where(x => x.Movement.Type == MovementType.Adjustment && x.Movement.TypeAdjustment == TypeAdjustment.Over).Sum(x => x.Movement.Quantity),
                    PositiveAdjTotalPrice = g.Where(x => x.Movement.Type == MovementType.Adjustment && x.Movement.TypeAdjustment == TypeAdjustment.Over)
                                             .Sum(x => x.Movement.TotalPrice > 0 ? x.Movement.TotalPrice : (x.Movement.Quantity * x.Movement.UnitPrice)),

                    NegativeAdjQuantity = g.Where(x => x.Movement.Type == MovementType.Adjustment && x.Movement.TypeAdjustment == TypeAdjustment.Lost).Sum(x => x.Movement.Quantity),
                    NegativeAdjTotalPrice = g.Where(x => x.Movement.Type == MovementType.Adjustment && x.Movement.TypeAdjustment == TypeAdjustment.Lost)
                                             .Sum(x => x.Movement.TotalPrice > 0 ? x.Movement.TotalPrice : (x.Movement.Quantity * x.Movement.UnitPrice)),

                    // Total Adjustment Count
                    AdjustmentCount = g.Count(x => x.Movement.Type == MovementType.Adjustment),
                    ReturnOutCount = g.Count(x => x.Movement.Type == MovementType.ReturnOut),
                    ReturnOutQuantity = g.Where(x => x.Movement.Type == MovementType.ReturnOut).Sum(x => x.Movement.Quantity),
                    ReturnOutTotalPrice = g.Where(x => x.Movement.Type == MovementType.ReturnOut)
                                           .Sum(x => x.Movement.TotalPrice > 0 ? x.Movement.TotalPrice : (x.Movement.Quantity * x.Movement.UnitPrice)),

                    ReturnInCount = g.Count(x => x.Movement.Type == MovementType.ReturnIn),
                    ReturnInQuantity = g.Where(x => x.Movement.Type == MovementType.ReturnIn).Sum(x => x.Movement.Quantity),
                    ReturnInTotalPrice = g.Where(x => x.Movement.Type == MovementType.ReturnIn)
                                          .Sum(x => x.Movement.TotalPrice > 0 ? x.Movement.TotalPrice : (x.Movement.Quantity * x.Movement.UnitPrice)),

                    // ===== Serialized breakdown =====
                    SerialInQty = g.Where(x => x.ProductType == ProductType.Serialized && x.Movement.Type == MovementType.In).Sum(x => x.Movement.Quantity),
                    SerialInPrice = g.Where(x => x.ProductType == ProductType.Serialized && x.Movement.Type == MovementType.In).Sum(x => x.Movement.TotalPrice),
                    SerialPosAdjQty = g.Where(x => x.ProductType == ProductType.Serialized && x.Movement.Type == MovementType.Adjustment && x.Movement.TypeAdjustment == TypeAdjustment.Over).Sum(x => x.Movement.Quantity),
                    SerialPosAdjPrice = g.Where(x => x.ProductType == ProductType.Serialized && x.Movement.Type == MovementType.Adjustment && x.Movement.TypeAdjustment == TypeAdjustment.Over)
                                         .Sum(x => x.Movement.TotalPrice > 0 ? x.Movement.TotalPrice : (x.Movement.Quantity * x.Movement.UnitPrice)),
                    SerialNegAdjQty = g.Where(x => x.ProductType == ProductType.Serialized && x.Movement.Type == MovementType.Adjustment && x.Movement.TypeAdjustment == TypeAdjustment.Lost).Sum(x => x.Movement.Quantity),
                    SerialNegAdjPrice = g.Where(x => x.ProductType == ProductType.Serialized && x.Movement.Type == MovementType.Adjustment && x.Movement.TypeAdjustment == TypeAdjustment.Lost)
                                         .Sum(x => x.Movement.TotalPrice > 0 ? x.Movement.TotalPrice : (x.Movement.Quantity * x.Movement.UnitPrice)),
                    SerialReturnInQty = g.Where(x => x.ProductType == ProductType.Serialized && x.Movement.Type == MovementType.ReturnIn).Sum(x => x.Movement.Quantity),
                    SerialReturnInPrice = g.Where(x => x.ProductType == ProductType.Serialized && x.Movement.Type == MovementType.ReturnIn)
                                           .Sum(x => x.Movement.TotalPrice > 0 ? x.Movement.TotalPrice : (x.Movement.Quantity * x.Movement.UnitPrice)),
                    SerialReturnOutQty = g.Where(x => x.ProductType == ProductType.Serialized && x.Movement.Type == MovementType.ReturnOut).Sum(x => x.Movement.Quantity),
                    SerialReturnOutPrice = g.Where(x => x.ProductType == ProductType.Serialized && x.Movement.Type == MovementType.ReturnOut)
                                            .Sum(x => x.Movement.TotalPrice > 0 ? x.Movement.TotalPrice : (x.Movement.Quantity * x.Movement.UnitPrice)),

                    // ===== Non-Serialized breakdown =====
                    NonSerialInQty = g.Where(x => x.ProductType == ProductType.NonSerialized && x.Movement.Type == MovementType.In).Sum(x => x.Movement.Quantity),
                    NonSerialInPrice = g.Where(x => x.ProductType == ProductType.NonSerialized && x.Movement.Type == MovementType.In).Sum(x => x.Movement.TotalPrice),
                    NonSerialPosAdjQty = g.Where(x => x.ProductType == ProductType.NonSerialized && x.Movement.Type == MovementType.Adjustment && x.Movement.TypeAdjustment == TypeAdjustment.Over).Sum(x => x.Movement.Quantity),
                    NonSerialPosAdjPrice = g.Where(x => x.ProductType == ProductType.NonSerialized && x.Movement.Type == MovementType.Adjustment && x.Movement.TypeAdjustment == TypeAdjustment.Over)
                                            .Sum(x => x.Movement.TotalPrice > 0 ? x.Movement.TotalPrice : (x.Movement.Quantity * x.Movement.UnitPrice)),
                    NonSerialNegAdjQty = g.Where(x => x.ProductType == ProductType.NonSerialized && x.Movement.Type == MovementType.Adjustment && x.Movement.TypeAdjustment == TypeAdjustment.Lost).Sum(x => x.Movement.Quantity),
                    NonSerialNegAdjPrice = g.Where(x => x.ProductType == ProductType.NonSerialized && x.Movement.Type == MovementType.Adjustment && x.Movement.TypeAdjustment == TypeAdjustment.Lost)
                                            .Sum(x => x.Movement.TotalPrice > 0 ? x.Movement.TotalPrice : (x.Movement.Quantity * x.Movement.UnitPrice)),
                    NonSerialReturnInQty = g.Where(x => x.ProductType == ProductType.NonSerialized && x.Movement.Type == MovementType.ReturnIn).Sum(x => x.Movement.Quantity),
                    NonSerialReturnInPrice = g.Where(x => x.ProductType == ProductType.NonSerialized && x.Movement.Type == MovementType.ReturnIn)
                                              .Sum(x => x.Movement.TotalPrice > 0 ? x.Movement.TotalPrice : (x.Movement.Quantity * x.Movement.UnitPrice)),
                    NonSerialReturnOutQty = g.Where(x => x.ProductType == ProductType.NonSerialized && x.Movement.Type == MovementType.ReturnOut).Sum(x => x.Movement.Quantity),
                    NonSerialReturnOutPrice = g.Where(x => x.ProductType == ProductType.NonSerialized && x.Movement.Type == MovementType.ReturnOut)
                                               .Sum(x => x.Movement.TotalPrice > 0 ? x.Movement.TotalPrice : (x.Movement.Quantity * x.Movement.UnitPrice)),
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (rawData == null)
            {
                return new StockInSummaryResult();
            }

            return new StockInSummaryResult
            {
                GrandTotalPrice = (rawData.StockInTotalPrice + rawData.PositiveAdjTotalPrice + rawData.ReturnInTotalPrice)
                                  - (rawData.NegativeAdjTotalPrice + rawData.ReturnOutTotalPrice),

                TotalQuantity = (rawData.StockInQuantity + rawData.PositiveAdjQuantity + rawData.ReturnInQuantity)
                                - (rawData.NegativeAdjQuantity + rawData.ReturnOutQuantity),

                StockInCount = rawData.StockInCount,
                StockOutCount = rawData.StockOutCount,
                AdjustmentCount = rawData.AdjustmentCount,
                ReturnOutCount = rawData.ReturnOutCount,
                ReturnInCount = rawData.ReturnInCount,

                TotalSerialPrice = (rawData.SerialInPrice + rawData.SerialPosAdjPrice + rawData.SerialReturnInPrice)
                                    - (rawData.SerialNegAdjPrice + rawData.SerialReturnOutPrice),
                TotalSerialQty = (rawData.SerialInQty + rawData.SerialPosAdjQty + rawData.SerialReturnInQty)
                                  - (rawData.SerialNegAdjQty + rawData.SerialReturnOutQty),

                TotalNonSerialPrice = (rawData.NonSerialInPrice + rawData.NonSerialPosAdjPrice + rawData.NonSerialReturnInPrice)
                                       - (rawData.NonSerialNegAdjPrice + rawData.NonSerialReturnOutPrice),
                TotalNonSerialQty = (rawData.NonSerialInQty + rawData.NonSerialPosAdjQty + rawData.NonSerialReturnInQty)
                                     - (rawData.NonSerialNegAdjQty + rawData.NonSerialReturnOutQty),
            };
        }
    }
}