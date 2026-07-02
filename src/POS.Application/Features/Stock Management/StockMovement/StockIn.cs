using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using POS.Domain.Entities.StockManagement;
using POS.Domain.Enums;

namespace POS.Application.Features.StockManagement.StockMovements
{
    public record StockInCommand : IRequest<ApiResponse<StockMovementInfo>>
    {
        public int ProductId { get; set; }
        public int SupplierId { get; set; }
        public int? Quantity { get; set; } 
        public string? Reference { get; set; }
        public string? Note { get; set; }
        public List<string>? SerialNumbers { get; set; }
    }

    public class StockInCommandValidator : AbstractValidator<StockInCommand>
    {
        private readonly IMyAppDbContext _context;
        public StockInCommandValidator(IMyAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Product is required.");
            RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("Supplier is required.");
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

    public class StockInCommandHandler : IRequestHandler<StockInCommand, ApiResponse<StockMovementInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public StockInCommandHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<StockMovementInfo>> Handle(StockInCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await ((DbContext)_context).Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId && !x.IsDeleted, cancellationToken);
                if (product == null) return ApiResponse<StockMovementInfo>.NotFound("Product not found.");

                var supplier = await _context.Suppliers.FirstOrDefaultAsync(x => x.Id == request.SupplierId && !x.IsDeleted, cancellationToken);
                if (supplier == null) return ApiResponse<StockMovementInfo>.NotFound("Supplier not found.");

                int actualQuantity;

                if (product.ProductType == ProductType.Serialized)
                {
                    if (request.SerialNumbers == null || !request.SerialNumbers.Any())
                        return ApiResponse<StockMovementInfo>.BadRequest("Serial numbers are required.");

                    var trimmedSerials = request.SerialNumbers.Select(s => s.Trim()).ToList();

                    var existingSerials = await _context.SerialStocks
                        .Where(x => trimmedSerials.Contains(x.SerialNo))
                        .Select(x => x.SerialNo).ToListAsync(cancellationToken);

                    if (existingSerials.Any())
                        return ApiResponse<StockMovementInfo>.BadRequest($"Serial numbers already exist: {string.Join(", ", existingSerials)}");

                    foreach (var serial in trimmedSerials)
                    {
                        _context.SerialStocks.Add(new SerialStock
                        {
                            ProductId = request.ProductId,
                            SerialNo = serial,
                            Status = SerialStatus.Available
                        });
                    }

                    actualQuantity = trimmedSerials.Count;
                }
                else
                {
                    if (!request.Quantity.HasValue || request.Quantity.Value <= 0)
                        return ApiResponse<StockMovementInfo>.BadRequest("Quantity is required.");

                    actualQuantity = request.Quantity.Value;

                    var nonSerial = await _context.NonSerialStocks
                        .FirstOrDefaultAsync(x => x.ProductId == request.ProductId && !x.IsDeleted, cancellationToken);

                    if (nonSerial == null)
                    {
                        _context.NonSerialStocks.Add(new NonSerialStock
                        {
                            ProductId = request.ProductId,
                            Quantity = actualQuantity
                        });
                    }
                    else
                    {
                        nonSerial.Quantity += actualQuantity;
                    }
                }

                var qtyBefore = product.StockQuantity;
                product.StockQuantity += actualQuantity;

                var movement = new Domain.Entities.StockManagement.StockMovement
                {
                    ProductId = request.ProductId,
                    SupplierId = request.SupplierId,
                    Type = MovementType.In,
                    Quantity = actualQuantity,
                    QuantityBefore = qtyBefore,
                    QuantityAfter = product.StockQuantity,
                    UnitPrice = product.CostPrice,
                    TotalPrice = product.CostPrice * actualQuantity,
                    Reference = request.Reference?.Trim(),
                    Note = request.Note?.Trim(),
                    CreatedDate = DateTimeOffset.UtcNow,
                    CreatedBy = _currentUserService.UserId
                };
                _context.StockMovements.Add(movement);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var currentUser = await _context.Persons.FirstOrDefaultAsync(x => x.Id == _currentUserService.UserId && !x.IsDeleted, cancellationToken);

                var res = new StockMovementInfo
                {
                    Id = movement.Id,
                    ProductId = movement.ProductId,
                    ProductName = product.Name,
                    SupplierId = movement.SupplierId,
                    SupplierName = supplier.Name,
                    Type = movement.Type.ToString(),
                    Quantity = movement.Quantity,
                    UnitPrice = movement.UnitPrice,
                    TotalPrice = movement.TotalPrice,
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

                return ApiResponse<StockMovementInfo>.Ok(res, "Stock IN successfully.");
            }
            catch { await transaction.RollbackAsync(cancellationToken); throw; }
        }
    }
}