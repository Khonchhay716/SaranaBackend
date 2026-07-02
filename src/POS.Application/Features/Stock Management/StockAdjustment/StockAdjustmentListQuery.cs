using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;

namespace POS.Application.Features.StockManagement.StockAdjustments
{
    public record GetStockAdjustmentByIdQuery : IRequest<ApiResponse<StockAdjustmentInfo>>
    {
        public int Id { get; set; }
    }

    public class GetStockAdjustmentByIdQueryHandler : IRequestHandler<GetStockAdjustmentByIdQuery, ApiResponse<StockAdjustmentInfo>>
    {
        private readonly IMyAppDbContext _context;

        public GetStockAdjustmentByIdQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<StockAdjustmentInfo>> Handle(GetStockAdjustmentByIdQuery request, CancellationToken cancellationToken)
        {
            // Using the same join logic as your list query to get the CreatedBy user info
            var query = from sa in _context.StockAdjustments.AsNoTracking()
                            .Include(x => x.Product)
                        where sa.Id == request.Id && !sa.IsDeleted
                        join p in _context.Persons on sa.CreatedBy equals p.Id into users
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
                        };

            var result = await query.FirstOrDefaultAsync(cancellationToken);

            if (result == null)
                return ApiResponse<StockAdjustmentInfo>.NotFound("Stock adjustment not found.");

            return ApiResponse<StockAdjustmentInfo>.Ok(result);
        }
    }
}