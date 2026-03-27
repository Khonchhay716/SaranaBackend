// POS.Application/Features/SerialNumber/SerialNumberCommands.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Features.Product;
using DomainSN = POS.Domain.Entities.SerialNumber;

namespace POS.Application.Features.SerialNumber
{
    // ✅ Shared mapper — call as Map(sn) directly in same namespace
    internal static class SNMapper
    {
        public static SerialNumberInfo Map(DomainSN s) => new()
        {
            Id          = s.Id,
            ProductId   = s.ProductId,
            SerialNo    = s.SerialNo,
            Status      = s.Status,
            Price       = s.Price,
            CostPrice   = s.CostPrice,
            CreatedDate = s.CreatedDate,
        };
    }


    // ── CREATE ──────────────────────────────────────────────────
    public record SerialNumberCreateCommand : IRequest<ApiResponse<SerialNumberInfo>>
    {
        public int     ProductId { get; set; }
        public string  SerialNo  { get; set; } = string.Empty;
        public decimal Price     { get; set; }
        public decimal CostPrice { get; set; }
    }

    public class SerialNumberCreateCommandValidator : AbstractValidator<SerialNumberCreateCommand>
    {
        public SerialNumberCreateCommandValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0);
            RuleFor(x => x.SerialNo).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0);
        }
    }

    public class SerialNumberCreateCommandHandler : IRequestHandler<SerialNumberCreateCommand, ApiResponse<SerialNumberInfo>>
    {
        private readonly IMyAppDbContext _context;

        public SerialNumberCreateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<SerialNumberInfo>> Handle(SerialNumberCreateCommand request, CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsDeleted, cancellationToken);
            if (product == null)
                return ApiResponse<SerialNumberInfo>.NotFound($"Product with id {request.ProductId} not found");
            if (!product.IsSerialNumber)
                return ApiResponse<SerialNumberInfo>.BadRequest("Product is not a serialized product");

            var duplicate = await _context.SerialNumbers
                .AnyAsync(s => s.ProductId == request.ProductId && s.SerialNo == request.SerialNo && !s.IsDeleted, cancellationToken);
            if (duplicate)
                return ApiResponse<SerialNumberInfo>.BadRequest($"Serial number '{request.SerialNo}' already exists for this product");

            var sn = new DomainSN
            {
                ProductId = request.ProductId,
                SerialNo  = request.SerialNo,
                Status    = "Available",
                Price     = request.Price,
                CostPrice = request.CostPrice,
            };

            _context.SerialNumbers.Add(sn);

            product.Stock      += 1;
            product.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<SerialNumberInfo>.Created(SNMapper.Map(sn), "Serial number created successfully");
        }
    }


    // ── UPDATE ──────────────────────────────────────────────────
    public record SerialNumberUpdateCommand : IRequest<ApiResponse<SerialNumberInfo>>
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public int     Id        { get; set; }
        public string  SerialNo  { get; set; } = string.Empty;
        public string? Status    { get; set; }
        public decimal Price     { get; set; }
        public decimal CostPrice { get; set; }
    }

    public class SerialNumberUpdateCommandValidator : AbstractValidator<SerialNumberUpdateCommand>
    {
        public SerialNumberUpdateCommandValidator()
        {
            RuleFor(x => x.SerialNo).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0);
            When(x => x.Status != null, () =>
                RuleFor(x => x.Status)
                    .Must(s => s == "Available" || s == "Sold")
                    .WithMessage("Status must be 'Available' or 'Sold'"));
        }
    }

    public class SerialNumberUpdateCommandHandler : IRequestHandler<SerialNumberUpdateCommand, ApiResponse<SerialNumberInfo>>
    {
        private readonly IMyAppDbContext _context;

        public SerialNumberUpdateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<SerialNumberInfo>> Handle(SerialNumberUpdateCommand request, CancellationToken cancellationToken)
        {
            var sn = await _context.SerialNumbers
                .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);
            if (sn == null)
                return ApiResponse<SerialNumberInfo>.NotFound($"Serial number with id {request.Id} not found");

            var duplicate = await _context.SerialNumbers
                .AnyAsync(s => s.ProductId == sn.ProductId && s.SerialNo == request.SerialNo
                            && s.Id != request.Id && !s.IsDeleted, cancellationToken);
            if (duplicate)
                return ApiResponse<SerialNumberInfo>.BadRequest($"Serial number '{request.SerialNo}' already exists for this product");

            sn.SerialNo    = request.SerialNo;
            sn.Price       = request.Price;
            sn.CostPrice   = request.CostPrice;
            sn.UpdatedDate = DateTimeOffset.UtcNow;

            if (request.Status != null)
                sn.Status = request.Status;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<SerialNumberInfo>.Ok(SNMapper.Map(sn), "Serial number updated successfully");
        }
    }


    // ── DELETE ──────────────────────────────────────────────────
    public record SerialNumberDeleteCommand(int Id) : IRequest<ApiResponse<bool>>;

    public class SerialNumberDeleteCommandHandler : IRequestHandler<SerialNumberDeleteCommand, ApiResponse<bool>>
    {
        private readonly IMyAppDbContext _context;

        public SerialNumberDeleteCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<bool>> Handle(SerialNumberDeleteCommand request, CancellationToken cancellationToken)
        {
            var sn = await _context.SerialNumbers
                .Include(s => s.Product)
                .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);
            if (sn == null)
                return ApiResponse<bool>.NotFound($"Serial number with id {request.Id} not found");

            if (sn.Status == "Sold")
                return ApiResponse<bool>.BadRequest("Cannot delete a sold serial number");

            sn.IsDeleted   = true;
            sn.DeletedDate = DateTimeOffset.UtcNow;

            if (sn.Product != null)
            {
                sn.Product.Stock       = Math.Max(0, sn.Product.Stock - 1);
                sn.Product.UpdatedDate = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.Ok(true, "Serial number deleted successfully");
        }
    }
}