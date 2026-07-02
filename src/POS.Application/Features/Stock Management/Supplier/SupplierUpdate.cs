using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System.Text.Json.Serialization;

namespace POS.Application.Features.StockManagement.Suppliers
{
    public record SupplierUpdateCommand : IRequest<ApiResponse<SupplierInfo>>
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
    }

    public class SupplierUpdateCommandValidator : AbstractValidator<SupplierUpdateCommand>
    {
        private readonly IMyAppDbContext _context;
        public SupplierUpdateCommandValidator(IMyAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(200)
                .MustAsync(async (command, name, cancellationToken) =>
                    !await _context.Suppliers.AnyAsync(x => x.Name == name && x.Id != command.Id && !x.IsDeleted, cancellationToken))
                .WithMessage("Supplier name already exists.");

            RuleFor(x => x.Phone)
                .MaximumLength(20)
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid email address.")
                .MaximumLength(200)
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Address)
                .MaximumLength(500)
                .When(x => !string.IsNullOrEmpty(x.Address));
        }
    }

    public class SupplierUpdateCommandHandler : IRequestHandler<SupplierUpdateCommand, ApiResponse<SupplierInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public SupplierUpdateCommandHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<SupplierInfo>> Handle(SupplierUpdateCommand request, CancellationToken cancellationToken)
        {
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (supplier == null)
                return ApiResponse<SupplierInfo>.NotFound($"Supplier with Id {request.Id} was not found.");

            supplier.Name = request.Name.Trim();
            supplier.Phone = request.Phone?.Trim();
            supplier.Email = request.Email?.Trim();
            supplier.Address = request.Address?.Trim();
            supplier.UpdatedDate = DateTimeOffset.UtcNow;
            supplier.UpdatedBy = _currentUserService.UserId;

            await _context.SaveChangesAsync(cancellationToken);

            var res = new SupplierInfo
            {
                Id = supplier.Id,
                Name = supplier.Name,
                Phone = supplier.Phone,
                Email = supplier.Email,
                Address = supplier.Address,
                CreatedDate = supplier.CreatedDate
            };

            return ApiResponse<SupplierInfo>.Ok(res, "Supplier updated successfully.");
        }
    }
}