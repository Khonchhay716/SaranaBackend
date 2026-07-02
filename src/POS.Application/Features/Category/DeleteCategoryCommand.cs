using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Category
{
    public record DeleteCategoryCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
    }

    public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeleteCategoryCommandHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, cancellationToken);

            if (category == null)
                return ApiResponse.NotFound($"Category with id {request.Id} not found");

            var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == request.Id && !p.IsDeleted, cancellationToken);
            if (hasProducts)
                return ApiResponse.BadRequest("Cannot delete category that has products");

            category.IsDeleted = true;
            category.DeletedDate = DateTimeOffset.UtcNow;
            category.DeletedBy = _currentUserService.UserId;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Category with id {request.Id} deleted successfully");
        }
    }
}