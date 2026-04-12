using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;

namespace POS.Application.Features.Leave
{
    public class MyLeaveRequestListQuery : PaginationRequest, IRequest<PaginatedResult<LeaveRequestInfo>>
    {
        public string? Status { get; set; }
        public int? Year { get; set; }
    }

    public class MyLeaveRequestListQueryValidator : AbstractValidator<MyLeaveRequestListQuery>
    {
        public MyLeaveRequestListQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    public class MyLeaveRequestListQueryHandler : IRequestHandler<MyLeaveRequestListQuery, PaginatedResult<LeaveRequestInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public MyLeaveRequestListQueryHandler(
            IMyAppDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<PaginatedResult<LeaveRequestInfo>> Handle(
            MyLeaveRequestListQuery request,
            CancellationToken cancellationToken)
        {
            // ✅ Get current StaffId from Person
            var userId = _currentUser.UserId;
            var person = await _context.Persons
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, cancellationToken);

            // ✅ Use -1 if no staff → query returns empty → ToPaginatedResultAsync correct
            var staffId = person?.StaffId ?? -1;

            var query = _context.LeaveRequests
                .AsNoTracking()
                .Include(x => x.Staff)
                .Include(x => x.LeaveType)
                .Include(x => x.Approver)
                .Where(x => !x.IsDeleted && x.StaffId == staffId);

            if (!string.IsNullOrWhiteSpace(request.Status))
                query = query.Where(x => x.Status == request.Status);

            if (request.Year.HasValue)
                query = query.Where(x => x.StartDate.Year == request.Year.Value);

            query = query.OrderByDescending(x => x.CreatedDate);

            var projected = query.Select(x => new LeaveRequestInfo
            {
                Id = x.Id,
                StaffId = x.StaffId,
                StaffName = x.Staff.FirstName + " " + x.Staff.LastName,
                StaffImage = x.Staff.ImageProfile,
                LeaveTypeId = x.LeaveTypeId,
                LeaveTypeName = x.LeaveType.Name,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                TotalDays = x.TotalDays,
                Reason = x.Reason,
                Status = x.Status,
                Session = x.Session,
                ApproverId = x.ApproverId,
                ApproverName = x.Approver != null
                    ? x.Approver.FirstName + " " + x.Approver.LastName
                    : null,
                ApprovedDate = x.ApprovedDate,
                ApprovalNote = x.ApprovalNote,
                IsDeleted = x.IsDeleted,
                CreatedDate = x.CreatedDate,
                CreatedBy = x.CreatedBy,
                UpdatedDate = x.UpdatedDate,
                UpdatedBy = x.UpdatedBy,
                DeletedDate = x.DeletedDate,
                DeletedBy = x.DeletedBy,
            });

            return await projected.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }

}