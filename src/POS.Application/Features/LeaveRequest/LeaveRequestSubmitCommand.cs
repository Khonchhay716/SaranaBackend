using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using DomainLeaveRequest = POS.Domain.Entities.LeaveRequest;

namespace POS.Application.Features.Leave
{
    public record LeaveRequestSubmitCommand : IRequest<ApiResponse<LeaveRequestInfo>>
    {
        public int LeaveTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Session { get; set; } = "FullDay"; // ✅ add
    }

    public class LeaveRequestSubmitCommandValidator : AbstractValidator<LeaveRequestSubmitCommand>
    {
        public LeaveRequestSubmitCommandValidator()
        {
            RuleFor(x => x.LeaveTypeId)
                .GreaterThan(0).WithMessage("Leave type is required.");
            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Start date is required.");
            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("End date is required.")
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("End date must be after or equal to start date.");
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Reason is required.")
                .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");
            RuleFor(x => x.Session)
                .Must(s => s == "FullDay" || s == "Morning" || s == "Afternoon")
                .WithMessage("Session must be 'FullDay', 'Morning', or 'Afternoon'.");
        }
    }

    public class LeaveRequestSubmitCommandHandler : IRequestHandler<LeaveRequestSubmitCommand, ApiResponse<LeaveRequestInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public LeaveRequestSubmitCommandHandler(
            IMyAppDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<ApiResponse<LeaveRequestInfo>> Handle(
            LeaveRequestSubmitCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId;
            if (userId == null)
                return ApiResponse<LeaveRequestInfo>.BadRequest(
                    "Unauthorized. Please login again.");

            var person = await _context.Persons
                .AsNoTracking()
                .Include(p => p.Staff)
                .FirstOrDefaultAsync(x => x.Id == userId.Value && !x.IsDeleted, cancellationToken);

            if (person == null)
                return ApiResponse<LeaveRequestInfo>.BadRequest("User not found.");

            if (person.StaffId == null || person.Staff == null)
                return ApiResponse<LeaveRequestInfo>.BadRequest(
                    "Your account is not linked to a staff profile.");

            if (person.Staff.SupervisorId == null)
                return ApiResponse<LeaveRequestInfo>.BadRequest(
                    "You have no supervisor assigned. Please contact HR.");

            var staffId      = person.StaffId.Value;
            var supervisorId = person.Staff.SupervisorId;

            var leaveType = await _context.LeaveTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.LeaveTypeId
                    && !x.IsDeleted && x.IsActive, cancellationToken);
            if (leaveType == null)
                return ApiResponse<LeaveRequestInfo>.NotFound(
                    $"Leave type with ID {request.LeaveTypeId} not found or inactive.");

            // ✅ Calculate working days with session logic
            var isFirstWorkingDay = true;
            decimal totalDays     = 0;

            for (var date = request.StartDate.Date; date <= request.EndDate.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek == DayOfWeek.Sunday) continue;

                if (request.Session == "FullDay")
                {
                    totalDays += 1.0m;
                }
                else
                {
                    // Morning/Afternoon: first working day = 0.5, rest = 1.0
                    totalDays += isFirstWorkingDay ? 0.5m : 1.0m;
                    isFirstWorkingDay = false;
                }
            }

            if (totalDays == 0)
                return ApiResponse<LeaveRequestInfo>.BadRequest(
                    "Leave request must include at least one working day.");

            // ✅ Check balance
            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(x => x.StaffId == staffId
                    && x.LeaveTypeId == request.LeaveTypeId
                    && x.Year == request.StartDate.Year
                    && !x.IsDeleted, cancellationToken);

            if (balance != null && balance.UsedDays + totalDays > balance.TotalDays)
                return ApiResponse<LeaveRequestInfo>.BadRequest(
                    $"Insufficient leave balance. Remaining: {balance.TotalDays - balance.UsedDays} days.");

            // ✅ Check overlapping
            var overlapping = await _context.LeaveRequests
                .AnyAsync(x => x.StaffId == staffId
                    && x.Status != "Rejected"
                    && !x.IsDeleted
                    && x.StartDate.Date <= request.EndDate.Date
                    && x.EndDate.Date >= request.StartDate.Date, cancellationToken);

            if (overlapping)
                return ApiResponse<LeaveRequestInfo>.BadRequest(
                    "You already have a leave request overlapping with these dates.");

            var entity = new DomainLeaveRequest
            {
                StaffId     = staffId,
                LeaveTypeId = request.LeaveTypeId,
                StartDate   = request.StartDate,
                EndDate     = request.EndDate,
                TotalDays   = totalDays,           // ✅ decimal
                Reason      = request.Reason.Trim(),
                Status      = "Pending",
                Session     = request.Session,     // ✅ save session
                ApproverId  = supervisorId,
                CreatedDate = DateTimeOffset.UtcNow,
            };

            _context.LeaveRequests.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            var created = await LoadLeaveRequestAsync(entity.Id, cancellationToken);
            return ApiResponse<LeaveRequestInfo>.Created(
                MapToInfo(created!),
                "Leave request submitted successfully.");
        }

        private Task<DomainLeaveRequest?> LoadLeaveRequestAsync(int id, CancellationToken ct) =>
            _context.LeaveRequests
                .Include(x => x.Staff)
                .Include(x => x.LeaveType)
                .Include(x => x.Approver)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

        internal static LeaveRequestInfo MapToInfo(DomainLeaveRequest x) => new()
        {
            Id            = x.Id,
            StaffId       = x.StaffId,
            StaffName     = x.Staff != null ? $"{x.Staff.FirstName} {x.Staff.LastName}" : "",
            StaffImage    = x.Staff?.ImageProfile ?? "",
            LeaveTypeId   = x.LeaveTypeId,
            LeaveTypeName = x.LeaveType?.Name ?? "",
            StartDate     = x.StartDate,
            EndDate       = x.EndDate,
            TotalDays     = x.TotalDays,
            Reason        = x.Reason,
            Status        = x.Status,
            Session       = x.Session,             // ✅ add
            ApproverId    = x.ApproverId,
            ApproverName  = x.Approver != null
                ? $"{x.Approver.FirstName} {x.Approver.LastName}"
                : null,
            ApprovedDate  = x.ApprovedDate,
            ApprovalNote  = x.ApprovalNote,
            IsDeleted     = x.IsDeleted,
            CreatedDate   = x.CreatedDate,
            CreatedBy     = x.CreatedBy,
            UpdatedDate   = x.UpdatedDate,
            UpdatedBy     = x.UpdatedBy,
            DeletedDate   = x.DeletedDate,
            DeletedBy     = x.DeletedBy,
        };
    }
}