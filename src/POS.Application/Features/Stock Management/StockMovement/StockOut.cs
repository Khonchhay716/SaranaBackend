using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using POS.Domain.Entities;
using POS.Domain.Entities.StockManagement;
using POS.Domain.Enums;
using System.Text.Json;

namespace POS.Application.Features.StockManagement.StockMovements
{
    public record StockOutCommand : IRequest<ApiResponse<StockMovementInfo>>
    {
        public int ProductId { get; set; }
        public int? Quantity { get; set; }
        public string? Reference { get; set; }
        public string? Note { get; set; }
        public List<string>? SerialNumbers { get; set; }

        // ✅ Set when this stock-out fulfills a sold order line (staff scanning serials
        // to hand a product out to the customer). Links the movement back to the order
        // and records the scanned serials on the OrderItem.
        public int? OrderItemId { get; set; }
    }

    public class StockOutCommandValidator : AbstractValidator<StockOutCommand>
    {
        private readonly IMyAppDbContext _context;
        public StockOutCommandValidator(IMyAppDbContext context)
        {
            _context = context;
            RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Product is required.");

            RuleFor(x => x.SerialNumbers)
               .NotNull().WithMessage("Serial numbers are required for serialized products.")
               .Must(x => x!.Any()).WithMessage("Serial numbers cannot be empty.")
               .WhenAsync(async (req, ct) =>
               {
                   var product = await _context.Products.FindAsync(new object[] { req.ProductId }, ct);
                   return product?.ProductType == ProductType.Serialized;
               });

            RuleFor(x => x.Quantity)
                .NotNull().WithMessage("Quantity is required.")
                .GreaterThan(0).WithMessage("Quantity must be greater than 0.")
                .WhenAsync(async (req, ct) =>
                {
                    var product = await _context.Products.FindAsync(new object[] { req.ProductId }, ct);
                    return product?.ProductType != ProductType.Serialized;
                });
        }
    }

    public class StockOutCommandHandler : IRequestHandler<StockOutCommand, ApiResponse<StockMovementInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public StockOutCommandHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<StockMovementInfo>> Handle(StockOutCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await ((DbContext)_context).Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId && !x.IsDeleted, cancellationToken);
                if (product == null) return ApiResponse<StockMovementInfo>.NotFound("Product not found.");

                // ✅ When this stock-out fulfills a sold order line, load and validate the
                // OrderItem up front so we can record the scanned serials on it below.
                OrderItem? orderItem = null;
                if (request.OrderItemId.HasValue)
                {
                    orderItem = await _context.OrderItems
                        .Include(oi => oi.Order)
                        .FirstOrDefaultAsync(oi => oi.Id == request.OrderItemId.Value, cancellationToken);

                    if (orderItem == null)
                        return ApiResponse<StockMovementInfo>.NotFound("Order item not found.");

                    if (orderItem.ProductId != request.ProductId)
                        return ApiResponse<StockMovementInfo>.BadRequest("Product does not match the order item.");

                    if (!string.IsNullOrEmpty(orderItem.SerialNumbers))
                        return ApiResponse<StockMovementInfo>.BadRequest("This order item has already been handed out.");
                }

                int actualQuantity;

                if (product.ProductType == ProductType.Serialized)
                {
                    if (request.SerialNumbers == null || !request.SerialNumbers.Any())
                        return ApiResponse<StockMovementInfo>.BadRequest("Serial numbers are required.");

                    var requestedSet = request.SerialNumbers.Select(s => s.Trim()).ToHashSet();

                    var serials = await _context.SerialStocks
                        .Where(x => requestedSet.Contains(x.SerialNo)
                            && x.ProductId == request.ProductId
                            && !x.IsDeleted
                            && x.Status == SerialStatus.Available)
                        .ToListAsync(cancellationToken);

                    var foundSet = serials.Select(s => s.SerialNo).ToHashSet();
                    var missingSerials = requestedSet.Except(foundSet).ToList();

                    if (missingSerials.Any())
                        return ApiResponse<StockMovementInfo>.BadRequest($"Serials not found or unavailable: {string.Join(", ", missingSerials)}");

                    if (orderItem != null && serials.Count != orderItem.Quantity)
                        return ApiResponse<StockMovementInfo>.BadRequest(
                            $"Scanned serial count does not match ordered quantity. Expected: {orderItem.Quantity}, Scanned: {serials.Count}.");

                    foreach (var serial in serials)
                    {
                        serial.Status = SerialStatus.Sold;
                    }
                    actualQuantity = serials.Count;

                    // ✅ Record the confirmed serials on the order line so Order Detail shows them.
                    if (orderItem != null)
                        orderItem.SerialNumbers = JsonSerializer.Serialize(serials.Select(s => s.SerialNo).ToList());
                }
                else
                {
                    if (orderItem != null)
                        return ApiResponse<StockMovementInfo>.BadRequest("Non-serialized items are fulfilled automatically at sale - no stock-out scan needed.");

                    if (!request.Quantity.HasValue || request.Quantity.Value <= 0)
                        return ApiResponse<StockMovementInfo>.BadRequest("Quantity is required.");

                    actualQuantity = request.Quantity.Value;

                    var nonSerial = await _context.NonSerialStocks
                        .FirstOrDefaultAsync(x => x.ProductId == request.ProductId && !x.IsDeleted, cancellationToken);

                    if (nonSerial == null || nonSerial.Quantity < actualQuantity)
                        return ApiResponse<StockMovementInfo>.BadRequest($"Insufficient stock. Available: {nonSerial?.Quantity ?? 0}");

                    nonSerial.Quantity -= actualQuantity;
                }

                if (product.StockQuantity < actualQuantity)
                    return ApiResponse<StockMovementInfo>.BadRequest($"Insufficient stock. Available: {product.StockQuantity}");

                var qtyBefore = product.StockQuantity;
                product.StockQuantity -= actualQuantity;

                var currentUser = await _context.Persons.FirstOrDefaultAsync(x => x.Id == _currentUserService.UserId && !x.IsDeleted, cancellationToken);

                var movement = new Domain.Entities.StockManagement.StockMovement
                {
                    ProductId = request.ProductId,
                    SupplierId = null,
                    OrderItemId = orderItem?.Id,
                    Type = MovementType.Out,
                    Quantity = actualQuantity,
                    QuantityBefore = qtyBefore,
                    QuantityAfter = product.StockQuantity,
                    UnitPrice = product.CostPrice,
                    TotalPrice = product.CostPrice * actualQuantity,
                    Reference = request.Reference?.Trim() ?? orderItem?.Order.OrderNo,
                    Note = request.Note?.Trim(),
                    CreatedBy = _currentUserService.UserId,
                    CreatedDate = DateTimeOffset.UtcNow,
                };
                _context.StockMovements.Add(movement);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var res = new StockMovementInfo
                {
                    Id = movement.Id,
                    ProductId = movement.ProductId,
                    ProductName = product.Name,
                    Type = movement.Type.ToString(),
                    Quantity = movement.Quantity,
                    QuantityBefore = movement.QuantityBefore,
                    QuantityAfter = movement.QuantityAfter,
                    Reference = movement.Reference,
                    Note = movement.Note,
                    CreatedDate = movement.CreatedDate,
                    CreatedBy = currentUser == null ? null : new TypeNamebase
                    {
                        Id = currentUser.Id,
                        Name = currentUser.Username
                    }
                };

                return ApiResponse<StockMovementInfo>.Ok(res, "Stock OUT successfully.");
            }
            catch { await transaction.RollbackAsync(cancellationToken); throw; }
        }
    }
}