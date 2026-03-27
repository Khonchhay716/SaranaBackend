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
 
        public DeleteCategoryCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }
 
        public async Task<ApiResponse> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, cancellationToken);
 
            if (category == null)
                return ApiResponse.NotFound($"Category with id {request.Id} not found");
 
            category.IsDeleted   = true;
            category.DeletedDate = DateTimeOffset.UtcNow;
            category.UpdatedDate = DateTimeOffset.UtcNow;
 
            await _context.SaveChangesAsync(cancellationToken);
 
            return ApiResponse.Ok($"Category with id {request.Id} deleted successfully");
        }
    }
}