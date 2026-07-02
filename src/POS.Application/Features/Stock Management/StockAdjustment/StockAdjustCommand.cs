using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities.StockManagement;
using POS.Domain.Enums;
namespace POS.Application.Features.StockManagement.StockAdjustments
{
    public record StockAdjustCommand : IRequest<ApiResponse<StockAdjustmentInfo>>
    {
        public int ProductId { get; set; }
        public TypeAdjustment TypeAdjustment { get; set; }
        public int? QualityAdjustment { get; set; } // ✅ Nullable — required តែសម្រាប់ Non-Serialized
        public AdjustmentReason Reason { get; set; }
        public string? Note { get; set; }
        public List<string>? SerialNumbers { get; set; }
    }

    public class StockAdjustCommandValidator : AbstractValidator<StockAdjustCommand>
    {
        private readonly IMyAppDbContext _context;
        public StockAdjustCommandValidator(IMyAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Product is required.");

            RuleFor(x => x.TypeAdjustment)
                .IsInEnum().WithMessage("Invalid adjustment type.");

            // ✅ Serialized: require SerialNumbers
            RuleFor(x => x.SerialNumbers)
                .NotNull().WithMessage("Serial numbers are required for serialized products.")
                .Must(x => x!.Any()).WithMessage("Serial numbers cannot be empty.")
                .WhenAsync(async (req, ct) =>
                {
                    var p = await _context.Products.FindAsync(new object[] { req.ProductId }, ct);
                    return p?.ProductType == ProductType.Serialized;
                });

            // ✅ Non-Serialized: require QualityAdjustment > 0
            RuleFor(x => x.QualityAdjustment)
                .NotNull().WithMessage("Quality adjustment is required.")
                .GreaterThan(0).WithMessage("Quality adjustment must be greater than 0.")
                .WhenAsync(async (req, ct) =>
                {
                    var p = await _context.Products.FindAsync(new object[] { req.ProductId }, ct);
                    return p?.ProductType != ProductType.Serialized;
                });
        }
    }

    public class StockAdjustCommandHandler : IRequestHandler<StockAdjustCommand, ApiResponse<StockAdjustmentInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public StockAdjustCommandHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<StockAdjustmentInfo>> Handle(StockAdjustCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await ((DbContext)_context).Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId && !x.IsDeleted, cancellationToken);
                if (product == null) return ApiResponse<StockAdjustmentInfo>.NotFound("Product not found.");

                var globalOldQty = product.StockQuantity;
                int warehouseOldQty = 0;
                int warehouseNewQty = 0;
                int diffQty = 0;
                int actualQualityAdjusted;

                if (product.ProductType == ProductType.Serialized)
                {
                    if (request.SerialNumbers == null || !request.SerialNumbers.Any())
                        return ApiResponse<StockAdjustmentInfo>.BadRequest("Serial numbers are required.");

                    warehouseOldQty = await _context.SerialStocks.CountAsync(x => x.ProductId == request.ProductId && x.Status == SerialStatus.Available && !x.IsDeleted, cancellationToken);

                    var requestedSet = request.SerialNumbers.Select(s => s.Trim()).ToHashSet();

                    var serials = await _context.SerialStocks
                        .Where(x => requestedSet.Contains(x.SerialNo) && x.ProductId == request.ProductId && !x.IsDeleted)
                        .ToListAsync(cancellationToken);

                    var foundSet = serials.Select(s => s.SerialNo.Trim()).ToHashSet();
                    var missingSerials = requestedSet.Except(foundSet).ToList();

                    if (missingSerials.Any())
                        return ApiResponse<StockAdjustmentInfo>.BadRequest($"Serials not found: {string.Join(", ", missingSerials)}");

                    var soldSerials = serials.Where(x => x.Status == SerialStatus.Sold).ToList();
                    if (soldSerials.Any())
                        return ApiResponse<StockAdjustmentInfo>.BadRequest(
                            $"Cannot adjust: serials already Sold: {string.Join(", ", soldSerials.Select(s => s.SerialNo))}");

                    if (request.TypeAdjustment == TypeAdjustment.Over)
                    {
                        var unavailableSerials = serials.Where(x => x.Status != SerialStatus.Available).ToList();
                        if (!unavailableSerials.Any())
                            return ApiResponse<StockAdjustmentInfo>.BadRequest("Selected serials are already Available. Cannot adjust Over.");

                        foreach (var serial in unavailableSerials)
                        {
                            serial.Status = SerialStatus.Available;
                        }

                        // ✅ Auto-count ពី serial — មិនប្រើ request.QualityAdjustment ទាល់តែសោះ
                        actualQualityAdjusted = unavailableSerials.Count;
                        diffQty = actualQualityAdjusted;
                    }
                    else // TypeAdjustment.Lost
                    {
                        var availableSerials = serials.Where(x => x.Status == SerialStatus.Available).ToList();
                        if (!availableSerials.Any())
                            return ApiResponse<StockAdjustmentInfo>.BadRequest("Selected serials are already Damaged/Lost. Cannot adjust Lost.");

                        foreach (var serial in availableSerials)
                        {
                            serial.Status = request.Reason == AdjustmentReason.Damaged ? SerialStatus.Damaged : SerialStatus.Lost;
                        }

                        // ✅ Auto-count ពី serial
                        actualQualityAdjusted = availableSerials.Count;
                        diffQty = -actualQualityAdjusted;
                    }

                    warehouseNewQty = warehouseOldQty + diffQty;
                }
                else
                {
                    if (!request.QualityAdjustment.HasValue || request.QualityAdjustment.Value <= 0)
                        return ApiResponse<StockAdjustmentInfo>.BadRequest("Quality adjustment is required.");

                    var nonSerial = await _context.NonSerialStocks.FirstOrDefaultAsync(x => x.ProductId == request.ProductId && !x.IsDeleted, cancellationToken);
                    if (nonSerial == null) return ApiResponse<StockAdjustmentInfo>.NotFound("Stock record not found.");

                    warehouseOldQty = nonSerial.Quantity;

                    diffQty = request.TypeAdjustment == TypeAdjustment.Over
                              ? request.QualityAdjustment.Value
                              : -request.QualityAdjustment.Value;

                    warehouseNewQty = warehouseOldQty + diffQty;

                    if (warehouseNewQty < 0)
                        return ApiResponse<StockAdjustmentInfo>.BadRequest($"Insufficient stock. Available: {warehouseOldQty}");

                    nonSerial.Quantity = warehouseNewQty;
                    actualQualityAdjusted = request.QualityAdjustment.Value;
                }

                product.StockQuantity += diffQty;

                var adjustment = new StockAdjustment
                {
                    ProductId = request.ProductId,
                    TypeAdjustment = request.TypeAdjustment,
                    QualityAdjustment = actualQualityAdjusted,
                    CostPrice = product.CostPrice,
                    QuantityBefore = globalOldQty,
                    QuantityAfter = product.StockQuantity,
                    Reason = request.Reason,
                    Note = request.Note?.Trim(),
                    CreatedBy = _currentUserService.UserId,
                    CreatedDate = DateTimeOffset.UtcNow,
                };
                _context.StockAdjustments.Add(adjustment);

                var movement = new Domain.Entities.StockManagement.StockMovement
                {
                    ProductId = request.ProductId,
                    SupplierId = null,
                    Type = MovementType.Adjustment,
                    TypeAdjustment = request.TypeAdjustment,
                    Quantity = Math.Abs(diffQty),
                    QuantityBefore = globalOldQty,
                    QuantityAfter = product.StockQuantity,
                    UnitPrice = product.CostPrice,
                    TotalPrice = product.CostPrice * Math.Abs(diffQty),
                    Note = $"Adjustment: {request.TypeAdjustment} - {request.Reason}. {request.Note?.Trim()}",
                    CreatedBy = _currentUserService.UserId,
                    CreatedDate = DateTimeOffset.UtcNow,
                };
                _context.StockMovements.Add(movement);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var res = new StockAdjustmentInfo
                {
                    Id = adjustment.Id,
                    ProductId = adjustment.ProductId,
                    ProductName = product.Name,
                    TypeAdjustment = adjustment.TypeAdjustment.ToString(),
                    QualityAdjustment = adjustment.QualityAdjustment,
                    CostPrice = adjustment.CostPrice,
                    QuantityBefore = adjustment.QuantityBefore,
                    QuantityAfter = adjustment.QuantityAfter,
                    Reason = adjustment.Reason.ToString(),
                    Note = adjustment.Note,
                    CreatedDate = adjustment.CreatedDate
                };

                return ApiResponse<StockAdjustmentInfo>.Ok(res, "Stock adjusted successfully.");
            }
            catch { await transaction.RollbackAsync(cancellationToken); throw; }
        }
    }
}