using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;

namespace POS.Application.Features.StockManagement.StockAdjustments
{
    public class StockAdjustmentListQuery : PaginationRequest, IRequest<PaginatedResult<StockAdjustmentInfo>>
    {
        public int? ProductId { get; set; }
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
    }

    public class StockAdjustmentListQueryHandler : IRequestHandler<StockAdjustmentListQuery, PaginatedResult<StockAdjustmentInfo>>
    {
        private readonly IMyAppDbContext _context;
        public StockAdjustmentListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<StockAdjustmentInfo>> Handle(StockAdjustmentListQuery request, CancellationToken cancellationToken)
        {
            var query = _context.StockAdjustments
                .AsNoTracking()
                .Include(x => x.Product)
                .AsQueryable();

            if (request.ProductId.HasValue)
                query = query.Where(x => x.ProductId == request.ProductId.Value);

            if (request.From.HasValue)
                query = query.Where(x => x.CreatedDate >= request.From.Value);

            if (request.To.HasValue)
                query = query.Where(x => x.CreatedDate <= request.To.Value);
            var result =
                (from sa in query
                 join p in _context.Persons
                     on sa.CreatedBy equals p.Id into users
                 from p in users.DefaultIfEmpty()
                 select new StockAdjustmentInfo
                 {
                     Id = sa.Id,
                     ProductId = sa.ProductId,
                     ProductName = sa.Product.Name,
                     TypeAdjustment = sa.TypeAdjustment.ToString(),
                     QualityAdjustment = sa.QualityAdjustment,
                     CostPrice = sa.CostPrice,
                     QuantityBefore = sa.QuantityBefore,
                     QuantityAfter = sa.QuantityAfter,
                     Reason = sa.Reason.ToString(),
                     Note = sa.Note,
                     CreatedDate = sa.CreatedDate,
                     CreatedBy = p == null
                         ? null
                         : new TypeNamebase
                         {
                             Id = p.Id,
                             Name = p.Username
                         }
                 })
                 .OrderByDescending(x => x.CreatedDate);

            return await result.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}