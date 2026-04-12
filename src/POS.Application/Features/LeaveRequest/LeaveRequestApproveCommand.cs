using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using static POS.Application.Features.Leave.LeaveRequestSubmitCommandHandler;

namespace POS.Application.Features.Leave
{
    public record LeaveRequestApproveCommand : IRequest<ApiResponse<LeaveRequestInfo>>
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public int Id { get; set; }
        public string? ApprovalNote { get; set; }
    }

    public class LeaveRequestApproveCommandHandler : IRequestHandler<LeaveRequestApproveCommand, ApiResponse<LeaveRequestInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public LeaveRequestApproveCommandHandler(
            IMyAppDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<ApiResponse<LeaveRequestInfo>> Handle(
            LeaveRequestApproveCommand request,
            CancellationToken cancellationToken)
        {
            // ✅ Get userId from token
            var userId = _currentUser.UserId;
            if (userId == null)
                return ApiResponse<LeaveRequestInfo>.BadRequest(
                    "Unauthorized. Please login again.");

            // ✅ Get StaffId from Person
            var person = await _context.Persons
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId.Value && !x.IsDeleted, cancellationToken);

            if (person == null)
                return ApiResponse<LeaveRequestInfo>.BadRequest("User not found.");

            if (person.StaffId == null)
                return ApiResponse<LeaveRequestInfo>.BadRequest(
                    "Current user is not linked to any staff profile.");

            var currentStaffId = person.StaffId.Value;

            var entity = await _context.LeaveRequests
                .Include(x => x.Staff)
                .Include(x => x.LeaveType)
                .Include(x => x.Approver)
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (entity == null)
                return ApiResponse<LeaveRequestInfo>.NotFound(
                    $"Leave request with ID {request.Id} not found.");

            if (entity.Status != "Pending")
                return ApiResponse<LeaveRequestInfo>.BadRequest(
                    $"Cannot approve a request with status '{entity.Status}'.");

            // ✅ Check chain — B and A both can approve C's request
            var isInChain = await IsInApprovalChainAsync(
                entity.StaffId,
                currentStaffId,
                cancellationToken);

            if (!isInChain)
                return ApiResponse<LeaveRequestInfo>.BadRequest(
                    "You are not authorized to approve this request.");

            entity.Status       = "Approved";
            entity.ApproverId   = currentStaffId;
            entity.ApprovedDate = DateTimeOffset.UtcNow;
            entity.ApprovalNote = request.ApprovalNote;
            entity.UpdatedDate  = DateTimeOffset.UtcNow;

            // ✅ Update or create leave balance
            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(x => x.StaffId == entity.StaffId
                    && x.LeaveTypeId == entity.LeaveTypeId
                    && x.Year == entity.StartDate.Year
                    && !x.IsDeleted, cancellationToken);

            if (balance != null)
            {
                // ✅ decimal + decimal — no type mismatch
                balance.UsedDays   += entity.TotalDays;
                balance.UpdatedDate = DateTimeOffset.UtcNow;
            }
            else
            {
                // ✅ Auto create balance
                _context.LeaveBalances.Add(new Domain.Entities.LeaveBalance
                {
                    StaffId     = entity.StaffId,
                    LeaveTypeId = entity.LeaveTypeId,
                    Year        = entity.StartDate.Year,
                    TotalDays   = entity.LeaveType.MaxDaysPerYear, // max from leave type
                    UsedDays    = entity.TotalDays,                // ✅ decimal
                    CreatedDate = DateTimeOffset.UtcNow,
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            // ✅ Reload with Approver after save
            var updated = await _context.LeaveRequests
                .Include(x => x.Staff)
                .Include(x => x.LeaveType)
                .Include(x => x.Approver)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == entity.Id, cancellationToken);

            return ApiResponse<LeaveRequestInfo>.Ok(
                MapToInfo(updated!),
                "Leave request approved successfully.");
        }

        private async Task<bool> IsInApprovalChainAsync(
            int staffId,
            int approverId,
            CancellationToken cancellationToken)
        {
            var visited   = new HashSet<int>();
            var currentId = (int?)staffId;

            while (currentId.HasValue)
            {
                if (visited.Contains(currentId.Value)) break;
                visited.Add(currentId.Value);

                var supervisorId = await _context.Staffs
                    .AsNoTracking()
                    .Where(s => s.Id == currentId.Value && !s.IsDeleted)
                    .Select(s => s.SupervisorId)
                    .FirstOrDefaultAsync(cancellationToken);

                currentId = supervisorId;

                if (currentId.HasValue && currentId.Value == approverId)
                    return true;
            }

            return false;
        }
    }
}