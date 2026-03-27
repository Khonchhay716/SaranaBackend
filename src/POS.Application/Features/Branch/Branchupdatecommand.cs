// POS.Application/Features/Branch/BranchUpdateCommand.cs
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Branch
{
    public record BranchUpdateCommand : IRequest<ApiResponse<BranchInfo>>
    {
        // ✅ Id is NOT in the request body — it is injected by the controller from the route
        [System.Text.Json.Serialization.JsonIgnore]
        public int Id { get; set; }

        public string  BranchName  { get; set; } = string.Empty;
        public string? Logo        { get; set; }
        public string  Status      { get; set; } = "Active";
        public string? Description { get; set; }
    }

    public class BranchUpdateCommandValidator : AbstractValidator<BranchUpdateCommand>
    {
        public BranchUpdateCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);

            RuleFor(x => x.BranchName)
                .NotEmpty().WithMessage("Branch name is required.")
                .MaximumLength(200);

            RuleFor(x => x.Status)
                .Must(s => s == "Active" || s == "Inactive")
                .WithMessage("Status must be 'Active' or 'Inactive'.");
        }
    }

    public class BranchUpdateCommandHandler : IRequestHandler<BranchUpdateCommand, ApiResponse<BranchInfo>>
    {
        private readonly IMyAppDbContext _context;

        public BranchUpdateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<BranchInfo>> Handle(
            BranchUpdateCommand request,
            CancellationToken   cancellationToken)
        {
            var branch = await _context.Branches
                .FirstOrDefaultAsync(b => b.Id == request.Id && !b.IsDeleted, cancellationToken);

            if (branch == null)
                return ApiResponse<BranchInfo>.NotFound($"Branch with id {request.Id} was not found.");

            var nameExists = await _context.Branches
                .AnyAsync(b => b.BranchName == request.BranchName
                            && b.Id != request.Id
                            && !b.IsDeleted, cancellationToken);
            if (nameExists)
                return ApiResponse<BranchInfo>.BadRequest($"Branch name '{request.BranchName}' is already used by another branch.");

            branch.BranchName  = request.BranchName;
            branch.Logo        = request.Logo;
            branch.Status      = request.Status;
            branch.Description = request.Description;
            branch.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<BranchInfo>.Ok(branch.Adapt<BranchInfo>(), "Branch updated successfully");
        }
    }
}