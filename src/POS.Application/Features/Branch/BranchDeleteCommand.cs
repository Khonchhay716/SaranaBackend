// POS.Application/Features/Branch/BranchDeleteCommand.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Branch
{
    public record BranchDeleteCommand(int Id) : IRequest<ApiResponse<bool>>;

    public class BranchDeleteCommandHandler : IRequestHandler<BranchDeleteCommand, ApiResponse<bool>>
    {
        private readonly IMyAppDbContext _context;

        public BranchDeleteCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<bool>> Handle(
            BranchDeleteCommand request,
            CancellationToken   cancellationToken)
        {
            var branch = await _context.Branches
                .FirstOrDefaultAsync(b => b.Id == request.Id && !b.IsDeleted, cancellationToken);

            if (branch == null)
                return ApiResponse<bool>.NotFound($"Branch with id {request.Id} was not found.");

            // Soft-delete — do NOT hard-delete so Product.BranchId FK remains valid
            branch.IsDeleted   = true;
            branch.DeletedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.Ok(true, "Branch deleted successfully");
        }
    }
}