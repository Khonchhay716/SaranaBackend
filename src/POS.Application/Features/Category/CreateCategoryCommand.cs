// POS.Application/Features/Category/CreateCategoryCommand.cs
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Category
{
    public class CreateCategoryCommand : IRequest<ApiResponse<CategoryInfo>>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Category name is required")
                .MinimumLength(2).WithMessage("Category name must be at least 2 characters")
                .MaximumLength(100).WithMessage("Category name must not exceed 100 characters");
        }
    }

    public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, ApiResponse<CategoryInfo>>
    {
        private readonly IMyAppDbContext _context;

        public CreateCategoryCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<CategoryInfo>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            // Check if category name already exists
            var exists = await _context.Categories
                .AnyAsync(c => c.Name == request.Name && !c.IsDeleted, cancellationToken);

            if (exists)
            {
                return ApiResponse<CategoryInfo>.BadRequest("Category name already exists");
            }

            // Use Adapt to create Category
            var category = request.Adapt<Domain.Entities.Category>();
            category.IsActive = true;
            category.CreatedDate = DateTimeOffset.UtcNow;
            category.IsDeleted = false;

            _context.Categories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            // Use Adapt for response
            var result = category.Adapt<CategoryInfo>();
            result.BookCount = 0;

            return ApiResponse<CategoryInfo>.Created(result, "Category created successfully");
        }
    }
}