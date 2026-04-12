using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Leave
{
    public record LeaveTypeUpdateCommand : IRequest<ApiResponse<LeaveTypeInfo>>
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MaxDaysPerYear { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class LeaveTypeUpdateCommandValidator : AbstractValidator<LeaveTypeUpdateCommand>
    {
        public LeaveTypeUpdateCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("A valid Leave Type ID is required.");
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Leave type name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
            RuleFor(x => x.MaxDaysPerYear)
                .GreaterThan(0).WithMessage("Max days per year must be greater than 0.");
        }
    }

    public class LeaveTypeUpdateCommandHandler : IRequestHandler<LeaveTypeUpdateCommand, ApiResponse<LeaveTypeInfo>>
    {
        private readonly IMyAppDbContext _context;

        public LeaveTypeUpdateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<LeaveTypeInfo>> Handle(
            LeaveTypeUpdateCommand request,
            CancellationToken cancellationToken)
        {
            var entity = await _context.LeaveTypes
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (entity == null)
                return ApiResponse<LeaveTypeInfo>.NotFound(
                    $"Leave type with ID {request.Id} not found.");

            var exists = await _context.LeaveTypes
                .AnyAsync(x => x.Name == request.Name
                            && x.Id != request.Id
                            && !x.IsDeleted, cancellationToken);
            if (exists)
                return ApiResponse<LeaveTypeInfo>.BadRequest(
                    $"Leave type '{request.Name}' already exists.");

            entity.Name           = request.Name.Trim();
            entity.MaxDaysPerYear = request.MaxDaysPerYear;
            entity.Description    = request.Description.Trim();
            entity.IsActive       = request.IsActive;
            entity.UpdatedDate    = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<LeaveTypeInfo>.Ok(
                LeaveTypeCreateCommandHandler.MapToInfo(entity),
                "Leave type updated successfully.");
        }
    }
}