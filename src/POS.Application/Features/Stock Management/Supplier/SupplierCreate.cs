using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;


namespace POS.Application.Features.StockManagement.Suppliers
{
    public record SupplierCreateCommand : IRequest<ApiResponse<SupplierInfo>>
    {
        public string Name { get; set; } = default!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
    }

    public class SupplierCreateCommandValidator : AbstractValidator<SupplierCreateCommand>
    {
        private readonly IMyAppDbContext _context;
        public SupplierCreateCommandValidator(IMyAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(200)
                .MustAsync(async (name, cancellationToken) =>
                    !await _context.Suppliers.AnyAsync(x => x.Name == name && !x.IsDeleted, cancellationToken))
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

    public class SupplierCreateCommandHandler : IRequestHandler<SupplierCreateCommand, ApiResponse<SupplierInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public SupplierCreateCommandHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<SupplierInfo>> Handle(SupplierCreateCommand request, CancellationToken cancellationToken)
        {
            var supplier = new Domain.Entities.StockManagement.Supplier
            {
                Name = request.Name.Trim(),
                Phone = request.Phone?.Trim(),
                Email = request.Email?.Trim(),
                Address = request.Address?.Trim(),
                IsDeleted = false,
                CreatedDate = DateTimeOffset.UtcNow,
                CreatedBy = _currentUserService.UserId
            };

            _context.Suppliers.Add(supplier);
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

            return ApiResponse<SupplierInfo>.Ok(res, "Supplier created successfully.");
        }
    }
}