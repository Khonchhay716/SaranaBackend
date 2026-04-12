using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;

namespace POS.Application.Features.Leave
{
    public class LeaveTypeQuery : IRequest<ApiResponse<LeaveTypeInfo>>
    {
        public int Id { get; set; }
    }

    public class LeaveTypeQueryValidator : AbstractValidator<LeaveTypeQuery>
    {
        public LeaveTypeQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("A valid Leave Type ID is required.");
        }
    }

    public class LeaveTypeQueryHandler : IRequestHandler<LeaveTypeQuery, ApiResponse<LeaveTypeInfo>>
    {
        private readonly IMyAppDbContext _context;

        public LeaveTypeQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<LeaveTypeInfo>> Handle(
            LeaveTypeQuery request,
            CancellationToken cancellationToken)
        {
            var entity = await _context.LeaveTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (entity == null)
                return ApiResponse<LeaveTypeInfo>.NotFound(
                    $"Leave type with ID {request.Id} not found.");

            return ApiResponse<LeaveTypeInfo>.Ok(
                LeaveTypeCreateCommandHandler.MapToInfo(entity),
                "Leave type retrieved successfully.");
        }
    }
}