using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Product
{
    public record ProductDeleteCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
    }

    public class ProductDeleteCommandHandler : IRequestHandler<ProductDeleteCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;

        public ProductDeleteCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse> Handle(ProductDeleteCommand request, CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (product == null)
            {
                return ApiResponse.NotFound($"Product with id {request.Id} was not found");
            }

            product.IsDeleted = true;
            product.DeletedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Product with id {request.Id} deleted successfully");
        }
    }
}