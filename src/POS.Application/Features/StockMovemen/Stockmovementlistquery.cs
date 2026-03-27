// POS.Application/Features/StockMovement/StockMovementListQuery.cs
// ✅ Same namespace as commands — no extra using needed in controller
using FluentValidation;
using MediatR;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Application.Features.Product;

namespace POS.Application.Features.StockMovement
{
    public class StockMovementListQuery : PaginationRequest, IRequest<PaginatedResult<StockMovementInfo>>
    {
        public int     ProductId { get; set; }
        public string? Type      { get; set; }  // "StockIn" | "StockOut" | null = all
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate   { get; set; }
    }

    public class StockMovementListQueryValidator : AbstractValidator<StockMovementListQuery>
    {
        public StockMovementListQueryValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0);
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    public class StockMovementListQueryHandler : IRequestHandler<StockMovementListQuery, PaginatedResult<StockMovementInfo>>
    {
        private readonly IMyAppDbContext _context;

        public StockMovementListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<StockMovementInfo>> Handle(StockMovementListQuery request, CancellationToken cancellationToken)
        {
            IQueryable<Domain.Entities.StockMovement> query = _context.StockMovements
                .Where(sm => sm.ProductId == request.ProductId && !sm.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.Type))
                query = query.Where(sm => sm.Type == request.Type);

            if (request.FromDate.HasValue)
                query = query.Where(sm => sm.MovementDate >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(sm => sm.MovementDate <= request.ToDate.Value);

            query = query.OrderByDescending(sm => sm.MovementDate);

            var projected = query.Select(sm => new StockMovementInfo
            {
                Id           = sm.Id,
                ProductId    = sm.ProductId,
                Type         = sm.Type,
                Quantity     = sm.Quantity,
                Price        = sm.Price,
                CostPrice    = sm.CostPrice,
                Notes        = sm.Notes,
                MovementDate = sm.MovementDate,
                CreatedDate  = sm.CreatedDate,
            });

            return await projected.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}