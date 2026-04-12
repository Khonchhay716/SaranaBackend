using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;

namespace POS.Application.Features.Customer
{
    public class CustomerQuery : IRequest<ApiResponse<CustomerInfo>>
    {
        public int Id { get; set; }
    }
    public class CustomerQueryValidator : AbstractValidator<CustomerQuery>
    {
        public CustomerQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("A valid Customer ID is required.");
        }
    }
    public class CustomerQueryHandler : IRequestHandler<CustomerQuery, ApiResponse<CustomerInfo>>
    {
        private readonly IMyAppDbContext _context;

        public CustomerQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<CustomerInfo>> Handle(
            CustomerQuery request,
            CancellationToken cancellationToken)
        {
            var customer = await _context.Customers
                .Include(c => c.Person)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, cancellationToken);
            if (customer == null)
            {
                return ApiResponse<CustomerInfo>.NotFound($"Customer with id {request.Id} was not found.");
            }
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

            return ApiResponse<CustomerInfo>.Ok(data, "Customer retrieved successfully");
        }
    }
}