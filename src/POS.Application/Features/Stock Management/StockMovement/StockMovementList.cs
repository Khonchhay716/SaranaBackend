using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using POS.Domain.Enums;

namespace POS.Application.Features.StockManagement.StockMovements
{
    public class StockMovementListQuery : PaginationRequest, IRequest<PaginatedResult<StockMovementInfo>>
    {
        public int? ProductId { get; set; }
        public int? SupplierId { get; set; }
        public MovementType? Type { get; set; }
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
    }

    public class StockMovementListQueryHandler : IRequestHandler<StockMovementListQuery, PaginatedResult<StockMovementInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public StockMovementListQueryHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<PaginatedResult<StockMovementInfo>> Handle(StockMovementListQuery request, CancellationToken cancellationToken)
        {
            var query = _context.StockMovements
                .AsNoTracking()
                .Include(x => x.Product)
                .Include(x => x.Supplier)
                .AsQueryable();

            if (request.ProductId.HasValue)
                query = query.Where(x => x.ProductId == request.ProductId.Value);

            if (request.SupplierId.HasValue)
                query = query.Where(x => x.SupplierId == request.SupplierId.Value);

            if (request.Type.HasValue)
                query = query.Where(x => x.Type == request.Type.Value);

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
                    SupplierId = sm.SupplierId,
                    SupplierName = sm.Supplier != null ? sm.Supplier.Name : null,
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