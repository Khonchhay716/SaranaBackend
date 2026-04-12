using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using DomainCustomer = POS.Domain.Entities.Customer;

namespace POS.Application.Features.Customer
{
    public record CustomerCreateCommand : IRequest<ApiResponse<CustomerInfo>>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ImageProfile { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
    }

    public class CustomerCreateCommandValidator : AbstractValidator<CustomerCreateCommand>
    {
        public CustomerCreateCommandValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(50);

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .MaximumLength(20);
        }
    }

    public class CustomerCreateCommandHandler : IRequestHandler<CustomerCreateCommand, ApiResponse<CustomerInfo>>
    {
        private readonly IMyAppDbContext _context;

        public CustomerCreateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<CustomerInfo>> Handle(
            CustomerCreateCommand request,
            CancellationToken cancellationToken)
        {
            var validator = new CustomerCreateCommandValidator();
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return ApiResponse<CustomerInfo>.BadRequest(errors);
            }

            var phoneExists = await _context.Customers
                .AnyAsync(c => c.PhoneNumber == request.PhoneNumber && !c.IsDeleted, cancellationToken);
            if (phoneExists)
                return ApiResponse<CustomerInfo>.BadRequest($"Phone number '{request.PhoneNumber}' already exists.");

            var customer = new DomainCustomer
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                ImageProfile = request.ImageProfile,
                PhoneNumber = request.PhoneNumber,
                Status = request.Status,
                TotalPoint = 0,
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync(cancellationToken);
            var data = customer.Adapt<CustomerInfo>();

            return ApiResponse<CustomerInfo>.Created(data, "Customer created successfully");
        }
    }
}