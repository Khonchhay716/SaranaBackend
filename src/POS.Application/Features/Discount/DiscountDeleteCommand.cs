using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Discount
{
    public record DiscountDeleteCommand(int Id) : IRequest<ApiResponse>;

    public class DiscountDeleteCommandHandler : IRequestHandler<DiscountDeleteCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DiscountDeleteCommandHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse> Handle(DiscountDeleteCommand request, CancellationToken cancellationToken)
        {
            var discount = await _context.Discounts
                .FirstOrDefaultAsync(d => d.Id == request.Id && !d.IsDeleted, cancellationToken);

            if (discount == null)
                return ApiResponse.NotFound($"Discount with id {request.Id} not found.");

            discount.IsDeleted = true;
            discount.DeletedDate = DateTimeOffset.UtcNow;
            discount.DeletedBy = _currentUserService.UserId;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok("Discount deleted successfully");
        }
    }
}