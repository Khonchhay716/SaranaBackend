using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using POS.Domain.Enums;

namespace POS.Application.Features.StockManagement.StockMovements
{
    // ✅ Dedicated listing for Stock-Out movements only (manual write-offs as well as
    // order-fulfillment scans). Kept separate from StockMovementListQuery, which now
    // excludes Type=Out entirely.
    public class StockOutListQuery : PaginationRequest, IRequest<PaginatedResult<StockMovementInfo>>
    {
        public int? ProductId { get; set; }
        public int? OrderItemId { get; set; }
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
    }

    public class StockOutListQueryHandler : IRequestHandler<StockOutListQuery, PaginatedResult<StockMovementInfo>>
    {
        private readonly IMyAppDbContext _context;
        public StockOutListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<StockMovementInfo>> Handle(StockOutListQuery request, CancellationToken cancellationToken)
        {
            var query = _context.StockMovements
                .AsNoTracking()
                .Include(x => x.Product)
                .Where(x => x.Type == MovementType.Out)
                .AsQueryable();

            if (request.ProductId.HasValue)
                query = query.Where(x => x.ProductId == request.ProductId.Value);

            if (request.OrderItemId.HasValue)
                query = query.Where(x => x.OrderItemId == request.OrderItemId.Value);

            if (request.From.HasValue)
                query = query.Where(x => x.CreatedDate >= request.From.Value);

            if (request.To.HasValue)
                query = query.Where(x => x.CreatedDate <= request.To.Value);

            query = query.OrderByDescending(x => x.CreatedDate);

            var result =
                from sm in query
                join p in _context.Persons
                    on sm.CreatedBy equals p.Id into users
                from p in users.DefaultIfEmpty()
                select new StockMovementInfo
                {
                    Id = sm.Id,
                    ProductId = sm.ProductId,
                    ProductName = sm.Product.Name,
                    Type = sm.Type.ToString(),
                    TypeAdjustment = string.IsNullOrEmpty(sm.TypeAdjustment.ToString())
                        ? null
                        : sm.TypeAdjustment.ToString(),
                    Quantity = sm.Quantity,
                    QuantityBefore = sm.QuantityBefore,
                    QuantityAfter = sm.QuantityAfter,
                    UnitPrice = sm.UnitPrice,
                    TotalPrice = sm.TotalPrice,
                    Reference = sm.Reference,
                    Note = sm.Note,
                    OrderItemId = sm.OrderItemId,
                    CreatedDate = sm.CreatedDate,
                    CreatedBy = p == null
                        ? null
                        : new TypeNamebase
                        {
                            Id = p.Id,
                            Name = p.Username
                        }
                };

            return await result.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}
