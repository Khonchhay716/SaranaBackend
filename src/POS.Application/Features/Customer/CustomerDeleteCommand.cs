using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Customer
{
    public record CustomerDeleteCommand(int Id) : IRequest<ApiResponse<bool>>;

    public class CustomerDeleteCommandHandler : IRequestHandler<CustomerDeleteCommand, ApiResponse<bool>>
    {
        private readonly IMyAppDbContext _context;

        public CustomerDeleteCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<bool>> Handle(
            CustomerDeleteCommand request,
            CancellationToken     cancellationToken)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, cancellationToken);

            if (customer == null)
                return ApiResponse<bool>.NotFound($"Customer with id {request.Id} was not found.");

            customer.IsDeleted   = true;
            customer.DeletedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.Ok(true, "Customer deleted successfully");
        }
    }
}