using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.StockManagement.Products
{
    public record ProductDeleteCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
    }

    public class ProductDeleteCommandHandler : IRequestHandler<ProductDeleteCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public ProductDeleteCommandHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse> Handle(ProductDeleteCommand request, CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (product == null)
                return ApiResponse.NotFound($"Product with Id {request.Id} was not found.");

            product.IsDeleted = true;
            product.DeletedDate = DateTimeOffset.UtcNow;
            product.DeletedBy = _currentUserService.UserId;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Product with Id {request.Id} was deleted.");
        }
    }
}