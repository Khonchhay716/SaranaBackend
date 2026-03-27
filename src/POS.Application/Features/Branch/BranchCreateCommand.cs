// POS.Application/Features/Branch/BranchCreateCommand.cs
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using DomainBranch = POS.Domain.Entities.Branch;

namespace POS.Application.Features.Branch
{
    public record BranchCreateCommand : IRequest<ApiResponse<BranchInfo>>
    {
        public string  BranchName  { get; set; } = string.Empty;
        public string? Logo        { get; set; }
        public string  Status      { get; set; } = "Active";
        public string? Description { get; set; }
    }

    public class BranchCreateCommandValidator : AbstractValidator<BranchCreateCommand>
    {
        public BranchCreateCommandValidator()
        {
            RuleFor(x => x.BranchName)
                .NotEmpty().WithMessage("Branch name is required.")
                .MaximumLength(200);

            RuleFor(x => x.Status)
                .Must(s => s == "Active" || s == "Inactive")
                .WithMessage("Status must be 'Active' or 'Inactive'.");
        }
    }

    public class BranchCreateCommandHandler : IRequestHandler<BranchCreateCommand, ApiResponse<BranchInfo>>
    {
        private readonly IMyAppDbContext _context;

        public BranchCreateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<BranchInfo>> Handle(
            BranchCreateCommand request,
            CancellationToken   cancellationToken)
        {
            // 1. Validate
            var validator        = new BranchCreateCommandValidator();
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return ApiResponse<BranchInfo>.BadRequest(errors);
            }

            // 2. Duplicate name check
            var nameExists = await _context.Branches
                .AnyAsync(b => b.BranchName == request.BranchName && !b.IsDeleted, cancellationToken);
            if (nameExists)
                return ApiResponse<BranchInfo>.BadRequest($"Branch name '{request.BranchName}' already exists.");

            // 3. Create
            var branch = new DomainBranch
            {
                BranchName  = request.BranchName,
                Logo        = request.Logo,
                Status      = request.Status,
                Description = request.Description,
            };

            _context.Branches.Add(branch);
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<BranchInfo>.Created(branch.Adapt<BranchInfo>(), "Branch created successfully");
        }
    }
}