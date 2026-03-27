// POS.Application/Features/StockMovement/StockMovementCommands.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Features.Product;
using DomainSM = POS.Domain.Entities.StockMovement;

namespace POS.Application.Features.StockMovement
{
    // ✅ Shared mapper
    internal static class SMMapper
    {
        public static StockMovementInfo Map(DomainSM sm) => new()
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
        };
    }


    // ── CREATE ──────────────────────────────────────────────────
    public record StockMovementCreateCommand : IRequest<ApiResponse<StockMovementInfo>>
    {
        public int     ProductId    { get; set; }
        public string  Type         { get; set; } = string.Empty; // "StockIn" | "StockOut" | "Adjustment"
        public int     Quantity     { get; set; }
        public decimal Price        { get; set; }
        public decimal CostPrice    { get; set; }
        public string? Notes        { get; set; }
        public DateTimeOffset? MovementDate { get; set; }
    }

    public class StockMovementCreateCommandValidator : AbstractValidator<StockMovementCreateCommand>
    {
        public StockMovementCreateCommandValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0);
            RuleFor(x => x.Type)
                .Must(t => t == "StockIn" || t == "StockOut" || t == "Adjustment")
                .WithMessage("Type must be 'StockIn', 'StockOut' or 'Adjustment'");
            RuleFor(x => x.Quantity).GreaterThan(0);
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0);
        }
    }

    public class StockMovementCreateCommandHandler : IRequestHandler<StockMovementCreateCommand, ApiResponse<StockMovementInfo>>
    {
        private readonly IMyAppDbContext _context;

        public StockMovementCreateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<StockMovementInfo>> Handle(StockMovementCreateCommand request, CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsDeleted, cancellationToken);
            if (product == null)
                return ApiResponse<StockMovementInfo>.NotFound($"Product with id {request.ProductId} not found");

            if (product.IsSerialNumber)
                return ApiResponse<StockMovementInfo>.BadRequest("Use serial number endpoints for serialized products");

            if (request.Type == "StockOut" && request.Quantity > product.Stock)
                return ApiResponse<StockMovementInfo>.BadRequest(
                    $"Cannot remove {request.Quantity} units. Only {product.Stock} in stock");

            var sm = new DomainSM
            {
                ProductId    = request.ProductId,
                Type         = request.Type,
                Quantity     = request.Quantity,
                Price        = request.Price,
                CostPrice    = request.CostPrice,
                Notes        = request.Notes,
                MovementDate = request.MovementDate ?? DateTimeOffset.UtcNow,
            };

            _context.StockMovements.Add(sm);

            if (request.Type == "StockIn")
                product.Stock += request.Quantity;
            else if (request.Type == "StockOut")
                product.Stock -= request.Quantity;
            else if (request.Type == "Adjustment")
                product.Stock = request.Quantity;

            product.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<StockMovementInfo>.Created(SMMapper.Map(sm), "Stock movement created successfully");
        }
    }


    // ── UPDATE ──────────────────────────────────────────────────
    public record StockMovementUpdateCommand : IRequest<ApiResponse<StockMovementInfo>>
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public int     Id           { get; set; }
        public string? Notes        { get; set; }
        public DateTimeOffset? MovementDate { get; set; }
    }

    public class StockMovementUpdateCommandHandler : IRequestHandler<StockMovementUpdateCommand, ApiResponse<StockMovementInfo>>
    {
        private readonly IMyAppDbContext _context;

        public StockMovementUpdateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<StockMovementInfo>> Handle(StockMovementUpdateCommand request, CancellationToken cancellationToken)
        {
            var sm = await _context.StockMovements
                .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);
            if (sm == null)
                return ApiResponse<StockMovementInfo>.NotFound($"Stock movement with id {request.Id} not found");

            if (request.Notes        != null) sm.Notes        = request.Notes;
            if (request.MovementDate != null) sm.MovementDate = request.MovementDate.Value;
            sm.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<StockMovementInfo>.Ok(SMMapper.Map(sm), "Stock movement updated successfully");
        }
    }


    // ── DELETE ──────────────────────────────────────────────────
    public record StockMovementDeleteCommand(int Id) : IRequest<ApiResponse<bool>>;

    public class StockMovementDeleteCommandHandler : IRequestHandler<StockMovementDeleteCommand, ApiResponse<bool>>
    {
        private readonly IMyAppDbContext _context;

        public StockMovementDeleteCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<bool>> Handle(StockMovementDeleteCommand request, CancellationToken cancellationToken)
        {
            var sm = await _context.StockMovements
                .Include(s => s.Product)
                .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);
            if (sm == null)
                return ApiResponse<bool>.NotFound($"Stock movement with id {request.Id} not found");

            sm.IsDeleted   = true;
            sm.DeletedDate = DateTimeOffset.UtcNow;

            if (sm.Product != null)
            {
                if (sm.Type == "StockIn")
                    sm.Product.Stock = Math.Max(0, sm.Product.Stock - sm.Quantity);
                else if (sm.Type == "StockOut")
                    sm.Product.Stock += sm.Quantity;

                sm.Product.UpdatedDate = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.Ok(true, "Stock movement deleted successfully");
        }
    }
}