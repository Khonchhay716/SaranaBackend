// CancelStockReturnCommand.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;
using System.Text.Json;

namespace POS.Application.Features.StockManagement.StockReturns
{
    public record CancelStockReturnCommand : IRequest<ApiResponse<StockReturnInfo>>
    {
        public int Id { get; set; }
        public string? CancellationNote { get; set; }
    }

    public class CancelStockReturnCommandValidator : AbstractValidator<CancelStockReturnCommand>
    {
        private readonly IMyAppDbContext _context;
        public CancelStockReturnCommandValidator(IMyAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID is required.");

            RuleFor(x => x.Id)
                .MustAsync(async (id, ct) =>
                {
                    var rtn = await _context.StockReturns
                        .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
                    return rtn != null && rtn.Status != ReturnStatus.Cancelled;
                })
                .WithMessage("Return not found or already cancelled.");
        }
    }

    public class CancelStockReturnCommandHandler : IRequestHandler<CancelStockReturnCommand, ApiResponse<StockReturnInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public CancelStockReturnCommandHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<StockReturnInfo>> Handle(CancelStockReturnCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await ((DbContext)_context).Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var stockReturn = await _context.StockReturns
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

                if (stockReturn == null)
                    return ApiResponse<StockReturnInfo>.NotFound("Return not found.");

                if (stockReturn.Status == ReturnStatus.Cancelled)
                    return ApiResponse<StockReturnInfo>.BadRequest("Return is already cancelled.");

                // ✅ Restore stock for each item
                foreach (var item in stockReturn.Items)
                {
                    var product = await _context.Products
                        .FirstOrDefaultAsync(x => x.Id == item.ProductId && !x.IsDeleted, cancellationToken);

                    if (product == null) continue;

                    var qtyBefore = product.StockQuantity;
                    product.StockQuantity += item.Quantity;

                    // ✅ Restore serial stocks
                    if (!string.IsNullOrEmpty(item.SerialNumbers))
                    {
                        var serialNos = JsonSerializer.Deserialize<List<string>>(item.SerialNumbers);
                        if (serialNos != null)
                        {
                            var serials = await _context.SerialStocks
                                .Where(x => serialNos.Contains(x.SerialNo)
                                    && x.ProductId == item.ProductId
                                    && x.Status == SerialStatus.Returned)
                                .ToListAsync(cancellationToken);

                            foreach (var serial in serials)
                                serial.Status = SerialStatus.Available;
                        }
                    }
                    else
                    {
                        var nonSerial = await _context.NonSerialStocks
                            .FirstOrDefaultAsync(x => x.ProductId == item.ProductId && !x.IsDeleted, cancellationToken);
                        if (nonSerial != null)
                            nonSerial.Quantity += item.Quantity;
                    }

                    // ✅ Create reversal movement
                    _context.StockMovements.Add(new Domain.Entities.StockManagement.StockMovement
                    {
                        ProductId = item.ProductId,
                        SupplierId = stockReturn.SupplierId,
                        Type = MovementType.ReturnIn,
                        Quantity = item.Quantity,
                        QuantityBefore = qtyBefore,
                        QuantityAfter = product.StockQuantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice,
                        Reference = $"CANCEL-{stockReturn.ReturnNo}",
                        Note = request.CancellationNote ?? "Return cancelled",
                        CreatedDate = DateTimeOffset.UtcNow,
                        CreatedBy = _currentUserService.UserId
                    });
                }

                // ✅ Update return status
                stockReturn.Status = ReturnStatus.Cancelled;
                stockReturn.CreatedDate = DateTimeOffset.UtcNow;
                stockReturn.CreatedBy = _currentUserService.UserId;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // Return updated info
                var result = await _context.StockReturns
                    .AsNoTracking()
                    .Include(x => x.Supplier)
                    .Include(x => x.Items).ThenInclude(x => x.Product)
                    .FirstOrDefaultAsync(x => x.Id == stockReturn.Id, cancellationToken);

                return ApiResponse<StockReturnInfo>.Ok(new StockReturnInfo
                {
                    Id = result!.Id,
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
                }, "Return cancelled successfully.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}