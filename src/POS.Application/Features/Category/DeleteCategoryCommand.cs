// POS.Application/Features/Category/DeleteCategoryCommand.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, cancellationToken);

            if (category == null)
            {
                return ApiResponse.NotFound($"Category with id {request.Id} not found");
            }

            // Check if category has books
            var hasBooks = category.Books.Any(b => !b.IsDeleted);
            if (hasBooks)
            {
                return ApiResponse.BadRequest("Cannot delete category with existing books. Please reassign or delete books first.");
            }

            // Soft delete
            category.IsDeleted = true;
            category.DeletedDate = DateTimeOffset.UtcNow;
            category.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Category with id {request.Id} deleted successfully");
        }
    }
}