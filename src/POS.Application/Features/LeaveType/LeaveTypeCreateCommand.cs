using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using DomainLeaveType = POS.Domain.Entities.LeaveType;

namespace POS.Application.Features.Leave
{
    public record LeaveTypeCreateCommand : IRequest<ApiResponse<LeaveTypeInfo>>
    {
        public string Name { get; set; } = string.Empty;
        public int MaxDaysPerYear { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class LeaveTypeCreateCommandValidator : AbstractValidator<LeaveTypeCreateCommand>
    {
        public LeaveTypeCreateCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Leave type name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
            RuleFor(x => x.MaxDaysPerYear)
                .GreaterThan(0).WithMessage("Max days per year must be greater than 0.");
        }
    }

    public class LeaveTypeCreateCommandHandler : IRequestHandler<LeaveTypeCreateCommand, ApiResponse<LeaveTypeInfo>>
    {
        private readonly IMyAppDbContext _context;

        public LeaveTypeCreateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<LeaveTypeInfo>> Handle(
            LeaveTypeCreateCommand request,
            CancellationToken cancellationToken)
        {
            var exists = await _context.LeaveTypes
                .AnyAsync(x => x.Name == request.Name && !x.IsDeleted, cancellationToken);
            if (exists)
                return ApiResponse<LeaveTypeInfo>.BadRequest(
                    $"Leave type '{request.Name}' already exists.");

            var entity = new DomainLeaveType
            {
                Name           = request.Name.Trim(),
                MaxDaysPerYear = request.MaxDaysPerYear,
                Description    = request.Description.Trim(),
                IsActive       = request.IsActive,
                CreatedDate    = DateTimeOffset.UtcNow,
            };

            _context.LeaveTypes.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<LeaveTypeInfo>.Created(
                MapToInfo(entity), "Leave type created successfully.");
        }

        internal static LeaveTypeInfo MapToInfo(DomainLeaveType x) => new()
        {
            Id             = x.Id,
            Name           = x.Name,
            MaxDaysPerYear = x.MaxDaysPerYear,
            Description    = x.Description,
            IsActive       = x.IsActive,
            IsDeleted      = x.IsDeleted,
            CreatedDate    = x.CreatedDate,
            CreatedBy      = x.CreatedBy,
            UpdatedDate    = x.UpdatedDate,
            UpdatedBy      = x.UpdatedBy,
            DeletedDate    = x.DeletedDate,
            DeletedBy      = x.DeletedBy,
        };
    }
}