using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.StockManagement.Suppliers
{
    public record SupplierDeleteCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
    }

    public class SupplierDeleteCommandHandler : IRequestHandler<SupplierDeleteCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public SupplierDeleteCommandHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse> Handle(SupplierDeleteCommand request, CancellationToken cancellationToken)
        {
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (supplier == null)
                return ApiResponse.NotFound($"Supplier with Id {request.Id} was not found.");

            supplier.IsDeleted   = true;
            supplier.DeletedDate = DateTimeOffset.UtcNow;
            supplier.DeletedBy   = _currentUserService.UserId;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Supplier with Id {request.Id} was deleted.");
        }
    }
}