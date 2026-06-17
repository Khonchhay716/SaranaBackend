using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Branch
{
    public record BranchDeleteCommand : IRequest<ApiResponse<BranchInfo>>
    {
        public int Id { get; set; }
    }

    public class BranchDeleteCommandHandler : IRequestHandler<BranchDeleteCommand, ApiResponse<BranchInfo>>
    {
        private readonly IMyAppDbContext _context;

        public BranchDeleteCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<BranchInfo>> Handle( BranchDeleteCommand request, CancellationToken   cancellationToken)
        {
            var branch = await _context.Branches
                .FirstOrDefaultAsync(b => b.Id == request.Id && !b.IsDeleted, cancellationToken);

            if (branch == null)
                return ApiResponse<BranchInfo>.NotFound($"Branch with id {request.Id} was not found.");

            branch.IsDeleted   = true;
            branch.DeletedDate = DateTimeOffset.UtcNow;
            var response = branch.Adapt<BranchInfo>();

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<BranchInfo>.Ok(response, "Branch deleted successfully");
        }
    }
}