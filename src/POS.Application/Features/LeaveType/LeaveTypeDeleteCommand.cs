using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Leave
{
    public record LeaveTypeDeleteCommand(int Id) : IRequest<ApiResponse<bool>>;

    public class LeaveTypeDeleteCommandHandler : IRequestHandler<LeaveTypeDeleteCommand, ApiResponse<bool>>
    {
        private readonly IMyAppDbContext _context;

        public LeaveTypeDeleteCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<bool>> Handle(
            LeaveTypeDeleteCommand request,
            CancellationToken cancellationToken)
        {
            var entity = await _context.LeaveTypes
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (entity == null)
                return ApiResponse<bool>.NotFound(
                    $"Leave type with ID {request.Id} not found.");

            var hasRequests = await _context.LeaveRequests
                .AnyAsync(x => x.LeaveTypeId == request.Id && !x.IsDeleted, cancellationToken);
            if (hasRequests)
                return ApiResponse<bool>.BadRequest(
                    "Cannot delete leave type that has existing leave requests.");

            entity.IsDeleted   = true;
            entity.DeletedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return ApiResponse<bool>.Ok(true, "Leave type deleted successfully.");
        }
    }
}