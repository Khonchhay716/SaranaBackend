using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
 
namespace POS.Application.Features.Category
{
    public class UpdateCategoryCommand : IRequest<ApiResponse<CategoryInfo>>
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public int     Id          { get; set; }
        public string? Name        { get; set; }
        public string? Description { get; set; }
        public string? Image       { get; set; }   // ✅ NEW
        public bool?   IsActive    { get; set; }
    }
 
    public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
    {
        public UpdateCategoryCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
 
            When(x => !string.IsNullOrEmpty(x.Name), () =>
            {
                RuleFor(x => x.Name)
                    .MinimumLength(2).WithMessage("Category name must be at least 2 characters")
                    .MaximumLength(100).WithMessage("Category name must not exceed 100 characters");
            });
        }
    }
 
    public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, ApiResponse<CategoryInfo>>
    {
        private readonly IMyAppDbContext _context;
 
        public UpdateCategoryCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }
 
        public async Task<ApiResponse<CategoryInfo>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, cancellationToken);
            if (category == null)
                return ApiResponse<CategoryInfo>.NotFound($"Category with id {request.Id} not found");
 
            if (!string.IsNullOrEmpty(request.Name) && category.Name != request.Name)
            {
                var nameExists = await _context.Categories
                    .AnyAsync(c => c.Name == request.Name && c.Id != request.Id && !c.IsDeleted, cancellationToken);
                if (nameExists)
                    return ApiResponse<CategoryInfo>.BadRequest("Category name already exists");
 
                category.Name = request.Name;
            }
 
            if (request.Description != null) category.Description = request.Description;
            if (request.Image       != null) category.Image       = request.Image;    // ✅ NEW
            if (request.IsActive.HasValue)   category.IsActive    = request.IsActive.Value;
 
            category.UpdatedDate = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
 
            return ApiResponse<CategoryInfo>.Ok(category.Adapt<CategoryInfo>(), "Category updated successfully");
        }
    }
}
 