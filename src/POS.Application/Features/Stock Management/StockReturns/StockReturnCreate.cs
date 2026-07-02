// CreateStockReturnCommand.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities.StockManagement;
using POS.Domain.Enums;
using System.Text.Json;

namespace POS.Application.Features.StockManagement.StockReturns
{
    public record StockReturnItemRequest
    {
        public int ProductId { get; set; }
        public int? Quantity { get; set; }
        public ReturnReason Reason { get; set; }
        public string? Note { get; set; }
        public List<string>? SerialNumbers { get; set; }
    }

    public record CreateStockReturnCommand : IRequest<ApiResponse<StockReturnInfo>>
    {
        public int SupplierId { get; set; }
        public string? Note { get; set; }
        public List<StockReturnItemRequest> Items { get; set; } = new();
    }

    public class CreateStockReturnCommandValidator : AbstractValidator<CreateStockReturnCommand>
    {
        private readonly IMyAppDbContext _context;
        public CreateStockReturnCommandValidator(IMyAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.SupplierId)
                .GreaterThan(0).WithMessage("Supplier is required.")
                .MustAsync(async (id, ct) =>
                    await _context.Suppliers.AnyAsync(x => x.Id == id && !x.IsDeleted, ct))
                .WithMessage("Supplier not found.");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Items are required.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Product is required.");

                item.RuleFor(x => x.SerialNumbers)
                    .NotNull().WithMessage("Serial numbers are required for serialized products.")
                    .Must(x => x!.Any()).WithMessage("Serial numbers cannot be empty.")
                    .WhenAsync(async (req, ct) =>
                    {
                        var p = await _context.Products.FindAsync(new object[] { req.ProductId }, ct);
                        return p?.ProductType == ProductType.Serialized;
                    });

                item.RuleFor(x => x.Quantity)
                    .NotNull().WithMessage("Quantity is required.")
                    .GreaterThan(0).WithMessage("Quantity must be greater than 0.")
                    .WhenAsync(async (req, ct) =>
                    {
                        var p = await _context.Products.FindAsync(new object[] { req.ProductId }, ct);
                        return p?.ProductType != ProductType.Serialized;
                    });
            });
        }
    }

    public class CreateStockReturnCommandHandler : IRequestHandler<CreateStockReturnCommand, ApiResponse<StockReturnInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public CreateStockReturnCommandHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<StockReturnInfo>> Handle(CreateStockReturnCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await ((DbContext)_context).Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var returnNo = $"RTN-{DateTime.UtcNow:yyyyMMddHHmmss}";
                decimal totalAmount = 0; 

                var stockReturn = new StockReturn
                {
                    ReturnNo = returnNo,
                    SupplierId = request.SupplierId,
                    Note = request.Note?.Trim(),
                    Status = ReturnStatus.Completed,
                    TotalAmount = 0, // Will be calculated
                    CreatedDate = DateTimeOffset.UtcNow,
                    CreatedBy = _currentUserService.UserId
                };

                foreach (var item in request.Items)
                {
                    var product = await _context.Products
                        .FirstOrDefaultAsync(x => x.Id == item.ProductId && !x.IsDeleted, cancellationToken);

                    if (product == null)
                        return ApiResponse<StockReturnInfo>.BadRequest($"Product ID {item.ProductId} not found.");

                    int actualQuantity;
                    var qtyBefore = product.StockQuantity;

                    if (product.ProductType == ProductType.Serialized)
                    {
                        if (item.SerialNumbers == null || !item.SerialNumbers.Any())
                            return ApiResponse<StockReturnInfo>.BadRequest($"Serial numbers are required for serialized product {product.Name}.");

                        var requestedSet = item.SerialNumbers.Select(s => s.Trim()).ToHashSet();

                        var serials = await _context.SerialStocks
                            .Where(x => requestedSet.Contains(x.SerialNo)
                                && x.ProductId == item.ProductId
                                && !x.IsDeleted
                                && x.Status == SerialStatus.Available)
                            .ToListAsync(cancellationToken);

                        var foundSet = serials.Select(s => s.SerialNo).ToHashSet();
                        var missingSerials = requestedSet.Except(foundSet).ToList();

                        if (missingSerials.Any())
                            return ApiResponse<StockReturnInfo>.BadRequest($"Invalid or unavailable serials for {product.Name}: {string.Join(", ", missingSerials)}");

                        foreach (var serial in serials)
                        {
                            serial.Status = SerialStatus.Returned;
                        }

                        actualQuantity = serials.Count;
                    }
                    else
                    {
                        if (!item.Quantity.HasValue || item.Quantity.Value <= 0)
                            return ApiResponse<StockReturnInfo>.BadRequest($"Quantity is required for {product.Name}.");

                        var nonSerial = await _context.NonSerialStocks
                            .FirstOrDefaultAsync(x => x.ProductId == item.ProductId && !x.IsDeleted, cancellationToken);

                        if (nonSerial == null || nonSerial.Quantity < item.Quantity.Value)
                            return ApiResponse<StockReturnInfo>.BadRequest($"Insufficient stock in warehouse for {product.Name}. Available: {nonSerial?.Quantity ?? 0}");

                        nonSerial.Quantity -= item.Quantity.Value;
                        actualQuantity = item.Quantity.Value;
                    }

                    if (product.StockQuantity < actualQuantity)
                        return ApiResponse<StockReturnInfo>.BadRequest($"Insufficient total stock for {product.Name}. Available: {product.StockQuantity}");

                    product.StockQuantity -= actualQuantity;

                    var unitPrice = product.CostPrice;
                    var itemTotalPrice = unitPrice * actualQuantity;
                    totalAmount += itemTotalPrice;

                    _context.StockMovements.Add(new Domain.Entities.StockManagement.StockMovement
                    {
                        ProductId = item.ProductId,
                        SupplierId = request.SupplierId,
                        Type = MovementType.ReturnOut,
                        Quantity = actualQuantity,
                        QuantityBefore = qtyBefore,
                        QuantityAfter = product.StockQuantity,
                        UnitPrice = unitPrice,
                        TotalPrice = itemTotalPrice,
                        Reference = returnNo,
                        Note = item.Note?.Trim(),
                        CreatedDate = DateTimeOffset.UtcNow,
                        CreatedBy = _currentUserService.UserId
                    });

                    stockReturn.Items.Add(new StockReturnItem
                    {
                        ProductId = item.ProductId,
                        Quantity = actualQuantity,
                        UnitPrice = unitPrice,   
                        TotalPrice = itemTotalPrice,
                        Reason = item.Reason,
                        Note = item.Note?.Trim(),
                        SerialNumbers = item.SerialNumbers != null && item.SerialNumbers.Any()
                            ? JsonSerializer.Serialize(item.SerialNumbers)
                            : null,
                        CreatedDate = DateTimeOffset.UtcNow,
                        CreatedBy = _currentUserService.UserId
                    });
                }

                stockReturn.TotalAmount = totalAmount;

                _context.StockReturns.Add(stockReturn);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return await GetReturnInfoAsync(stockReturn.Id, cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private async Task<ApiResponse<StockReturnInfo>> GetReturnInfoAsync(int id, CancellationToken ct)
        {
            var result = await _context.StockReturns
                .AsNoTracking()
                .Include(x => x.Supplier)
                .Include(x => x.Items).ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (result == null)
                return ApiResponse<StockReturnInfo>.NotFound("Return not found.");

            var res = new StockReturnInfo
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

            return ApiResponse<StockReturnInfo>.Ok(res, "Stock returned successfully.");
        }
    }
}