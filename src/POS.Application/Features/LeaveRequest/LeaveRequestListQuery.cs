using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Leave
{
    // ===== ALL LEAVE REQUESTS (supervisor sees subordinates) =====
    public class LeaveRequestListQuery : PaginationRequest, IRequest<PaginatedResult<LeaveRequestInfo>>
    {
        public string? Status { get; set; }
        public int? LeaveTypeId { get; set; }
        public int? Year { get; set; }
    }

    public class LeaveRequestListQueryValidator : AbstractValidator<LeaveRequestListQuery>
    {
        public LeaveRequestListQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    public class LeaveRequestListQueryHandler : IRequestHandler<LeaveRequestListQuery, PaginatedResult<LeaveRequestInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public LeaveRequestListQueryHandler(
            IMyAppDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<PaginatedResult<LeaveRequestInfo>> Handle(
            LeaveRequestListQuery request,
            CancellationToken cancellationToken)
        {
            // ✅ Get current StaffId from Person
            var userId = _currentUser.UserId;
            var person = await _context.Persons
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, cancellationToken);

            // ✅ Get subordinate IDs — empty set if no staff profile
            var subordinateIds = person?.StaffId != null
                ? await GetAllSubordinateIdsAsync(person.StaffId.Value, cancellationToken)
                : new HashSet<int>();

            // ✅ Always go through ToPaginatedResultAsync — handles empty correctly
            var query = _context.LeaveRequests
                .AsNoTracking()
                .Include(x => x.Staff)
                .Include(x => x.LeaveType)
                .Include(x => x.Approver)
                .Where(x => !x.IsDeleted
                         && subordinateIds.Contains(x.StaffId));

            if (!string.IsNullOrWhiteSpace(request.Status))
                query = query.Where(x => x.Status == request.Status);

            if (request.LeaveTypeId.HasValue)
                query = query.Where(x => x.LeaveTypeId == request.LeaveTypeId.Value);

            if (request.Year.HasValue)
                query = query.Where(x => x.StartDate.Year == request.Year.Value);

            query = query.OrderByDescending(x => x.CreatedDate);

            var projected = query.Select(x => new LeaveRequestInfo
            {
                Id            = x.Id,
                StaffId       = x.StaffId,
                StaffName     = x.Staff.FirstName + " " + x.Staff.LastName,
                StaffImage    = x.Staff.ImageProfile,
                LeaveTypeId   = x.LeaveTypeId,
                LeaveTypeName = x.LeaveType.Name,
                StartDate     = x.StartDate,
                EndDate       = x.EndDate,
                TotalDays     = x.TotalDays,
                Reason        = x.Reason,
                Status        = x.Status,
                Session = x.Session,
                ApproverId    = x.ApproverId,
                ApproverName  = x.Approver != null
                    ? x.Approver.FirstName + " " + x.Approver.LastName
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
            });

            return await projected.ToPaginatedResultAsync(request.Page, request.PageSize);
        }

        // ✅ Walk down tree — collect all subordinate IDs recursively
        private async Task<HashSet<int>> GetAllSubordinateIdsAsync(
            int supervisorId,
            CancellationToken cancellationToken)
        {
            var result  = new HashSet<int>();
            var toCheck = new Queue<int>();
            toCheck.Enqueue(supervisorId);

            while (toCheck.Any())
            {
                var currentId = toCheck.Dequeue();

                var directSubs = await _context.Staffs
                    .AsNoTracking()
                    .Where(s => s.SupervisorId == currentId && !s.IsDeleted)
                    .Select(s => s.Id)
                    .ToListAsync(cancellationToken);

                foreach (var subId in directSubs)
                {
                    if (result.Contains(subId)) continue;
                    result.Add(subId);
                    toCheck.Enqueue(subId);
                }
            }

            return result;
        }
    }
   
}