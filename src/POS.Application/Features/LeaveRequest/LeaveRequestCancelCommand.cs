using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Leave
{
    public record LeaveRequestCancelCommand(int Id) : IRequest<ApiResponse<bool>>;

    public class LeaveRequestCancelCommandHandler : IRequestHandler<LeaveRequestCancelCommand, ApiResponse<bool>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public LeaveRequestCancelCommandHandler(
            IMyAppDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<ApiResponse<bool>> Handle(
            LeaveRequestCancelCommand request,
            CancellationToken cancellationToken)
        {
            // ✅ Get userId from token
            var userId = _currentUser.UserId;
            if (userId == null)
                return ApiResponse<bool>.BadRequest("Unauthorized. Please login again.");

            // ✅ Get StaffId from Person
            var person = await _context.Persons
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId.Value && !x.IsDeleted, cancellationToken);

            if (person == null)
                return ApiResponse<bool>.BadRequest("User not found.");

            if (person.StaffId == null)
                return ApiResponse<bool>.BadRequest(
                    "Current user is not linked to any staff profile.");

            var entity = await _context.LeaveRequests
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (entity == null)
                return ApiResponse<bool>.NotFound(
                    $"Leave request with ID {request.Id} not found.");

            // ✅ Only owner can cancel
            if (entity.StaffId != person.StaffId.Value)
                return ApiResponse<bool>.BadRequest(
                    "You can only cancel your own leave request.");

            if (entity.Status != "Pending")
                return ApiResponse<bool>.BadRequest(
                    "Only pending requests can be cancelled.");

            entity.IsDeleted   = true;
            entity.DeletedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return ApiResponse<bool>.Ok(true, "Leave request cancelled successfully.");
        }
    }
}