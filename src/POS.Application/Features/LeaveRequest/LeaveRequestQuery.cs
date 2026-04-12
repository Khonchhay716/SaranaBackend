using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using static POS.Application.Features.Leave.LeaveRequestSubmitCommandHandler;

namespace POS.Application.Features.Leave
{
    public class LeaveRequestQuery : IRequest<ApiResponse<LeaveRequestInfo>>
    {
        public int Id { get; set; }
    }

    public class LeaveRequestQueryValidator : AbstractValidator<LeaveRequestQuery>
    {
        public LeaveRequestQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("A valid Leave Request ID is required.");
        }
    }

    public class LeaveRequestQueryHandler : IRequestHandler<LeaveRequestQuery, ApiResponse<LeaveRequestInfo>>
    {
        private readonly IMyAppDbContext _context;

        public LeaveRequestQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<LeaveRequestInfo>> Handle(
            LeaveRequestQuery request,
            CancellationToken cancellationToken)
        {
            var entity = await _context.LeaveRequests
                .AsNoTracking()
                .Include(x => x.Staff)
                .Include(x => x.LeaveType)
                .Include(x => x.Approver)
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (entity == null)
                return ApiResponse<LeaveRequestInfo>.NotFound(
                    $"Leave request with ID {request.Id} not found.");

            // ✅ Use static MapToInfo from LeaveRequestSubmitCommandHandler
            return ApiResponse<LeaveRequestInfo>.Ok(
                MapToInfo(entity),
                "Leave request retrieved successfully.");
        }
    }
}