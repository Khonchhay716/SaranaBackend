// GetStockReturnByIdQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System.Text.Json;

namespace POS.Application.Features.StockManagement.StockReturns
{
    public record GetStockReturnByIdQuery : IRequest<ApiResponse<StockReturnInfo>>
    {
        public int Id { get; set; }
    }

    public class GetStockReturnByIdQueryHandler : IRequestHandler<GetStockReturnByIdQuery, ApiResponse<StockReturnInfo>>
    {
        private readonly IMyAppDbContext _context;
        public GetStockReturnByIdQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<StockReturnInfo>> Handle(GetStockReturnByIdQuery request, CancellationToken cancellationToken)
        {
            var result = await _context.StockReturns
                .AsNoTracking()
                .Include(x => x.Supplier)
                .Include(x => x.Items)
                    .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (result == null)
                return ApiResponse<StockReturnInfo>.NotFound("Return not found.");

            var info = new StockReturnInfo
            {
                Id = result.Id,
                ReturnNo = result.ReturnNo,
                SupplierId = result.SupplierId,
                SupplierName = result.Supplier.Name,
                Note = result.Note,
                TotalAmount = result.TotalAmount,
                Status = result.Status.ToString(),
                CreatedDate = result.CreatedDate,
                Items = result.Items.Select(x => new StockReturnItemInfo
                {
                    Id = x.Id,
                    ProductId = x.ProductId,
                    ProductName = x.Product.Name,
                    ProductCode = x.Product.Code,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    TotalPrice = x.TotalPrice,
                    Reason = x.Reason.ToString(),
                    Note = x.Note,
                    SerialNumbers = !string.IsNullOrEmpty(x.SerialNumbers)
                        ? JsonSerializer.Deserialize<List<string>>(x.SerialNumbers)
                        : null
                }).ToList()
            };

            return ApiResponse<StockReturnInfo>.Ok(info);
        }
    }
}