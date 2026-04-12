using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;

namespace POS.Application.Features.Customer
{
    public record CustomerUpdateCommand : IRequest<ApiResponse<CustomerInfo>>
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ImageProfile { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
    }

    public class CustomerUpdateCommandValidator : AbstractValidator<CustomerUpdateCommand>
    {
        public CustomerUpdateCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);

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

    public class CustomerUpdateCommandHandler : IRequestHandler<CustomerUpdateCommand, ApiResponse<CustomerInfo>>
    {
        private readonly IMyAppDbContext _context;

        public CustomerUpdateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<CustomerInfo>> Handle(
            CustomerUpdateCommand request,
            CancellationToken cancellationToken)
        {
            var customer = await _context.Customers
                .Include(c => c.Person) 
                .FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, cancellationToken);

            if (customer == null)
                return ApiResponse<CustomerInfo>.NotFound($"Customer with id {request.Id} was not found.");

            var phoneExists = await _context.Customers
                .AnyAsync(c => c.PhoneNumber == request.PhoneNumber
                            && c.Id != request.Id
                            && !c.IsDeleted, cancellationToken);

            if (phoneExists)
                return ApiResponse<CustomerInfo>.BadRequest($"Phone number '{request.PhoneNumber}' is already used.");

            request.Adapt(customer);

            customer.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            var data = customer.Adapt<CustomerInfo>();
            
            if (customer.Person != null && !customer.Person.IsDeleted)
            {
                data.User = new LinkedUserInfo
                {
                    Id = customer.Person.Id,
                    Username = customer.Person.Username,
                    Email = customer.Person.Email,
                    IsActive = customer.Person.IsActive
                };
            }

            return ApiResponse<CustomerInfo>.Ok(data, "Customer updated successfully");
        }
    }
}