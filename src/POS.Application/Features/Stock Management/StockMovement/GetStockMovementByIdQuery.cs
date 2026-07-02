using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;

namespace POS.Application.Features.StockManagement.StockMovements
{
    public record GetStockMovementByIdQuery : IRequest<ApiResponse<StockMovementInfo>>
    {
        public int Id { get; set; }
    }

    public class GetStockMovementByIdQueryHandler : IRequestHandler<GetStockMovementByIdQuery, ApiResponse<StockMovementInfo>>
    {
        private readonly IMyAppDbContext _context;

        public GetStockMovementByIdQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<StockMovementInfo>> Handle(GetStockMovementByIdQuery request, CancellationToken cancellationToken)
        {
            // Using the exact same join logic as your list query to get the CreatedBy user info
            var query = from sm in _context.StockMovements.AsNoTracking()
                            .Include(x => x.Product)
                            .Include(x => x.Supplier)
                        where sm.Id == request.Id && !sm.IsDeleted
                        join p in _context.Persons on sm.CreatedBy equals p.Id into users
                        from p in users.DefaultIfEmpty()
                        select new StockMovementInfo
                        {
                            Id = sm.Id,
                            ProductId = sm.ProductId,
                            ProductName = sm.Product.Name,
                            SupplierId = sm.SupplierId,
                            SupplierName = sm.Supplier != null ? sm.Supplier.Name : null,
                            Type = sm.Type.ToString(),
                            TypeAdjustment = sm.TypeAdjustment == null
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

            var result = await query.FirstOrDefaultAsync(cancellationToken);

            if (result == null)
                return ApiResponse<StockMovementInfo>.NotFound("Stock movement not found.");

            return ApiResponse<StockMovementInfo>.Ok(result);
        }
    }
}