// using FluentValidation;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Dto;
// using POS.Application.Common.Interfaces;
// using DomainStaff = POS.Domain.Entities.Staff;

// namespace POS.Application.Features.Staff
// {
//     public record StaffCreateCommand : IRequest<ApiResponse<StaffInfo>>
//     {
//         public string FirstName { get; set; } = string.Empty;
//         public string LastName { get; set; } = string.Empty;
//         public string ImageProfile { get; set; } = string.Empty;
//         public string PhoneNumber { get; set; } = string.Empty;
//         public string Position { get; set; } = string.Empty;
//         public decimal Salary { get; set; }
//         public bool Status { get; set; } = true;
//     }

//     public class StaffCreateCommandValidator : AbstractValidator<StaffCreateCommand>
//     {
//         public StaffCreateCommandValidator()
//         {
//             RuleFor(x => x.FirstName)
//                 .NotEmpty().WithMessage("First name is required.")
//                 .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

//             RuleFor(x => x.LastName)
//                 .NotEmpty().WithMessage("Last name is required.")
//                 .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

//             RuleFor(x => x.PhoneNumber)
//                 .NotEmpty().WithMessage("Phone number is required.")
//                 .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.");

//             RuleFor(x => x.Position)
//                 .NotEmpty().WithMessage("Position is required.")
//                 .MaximumLength(100).WithMessage("Position must not exceed 100 characters.");

//             RuleFor(x => x.Salary)
//                 .GreaterThanOrEqualTo(0).WithMessage("Salary must be 0 or greater.");
//         }
//     }

//     public class StaffCreateCommandHandler : IRequestHandler<StaffCreateCommand, ApiResponse<StaffInfo>>
//     {
//         private readonly IMyAppDbContext _context;

//         public StaffCreateCommandHandler(IMyAppDbContext context)
//         {
//             _context = context;
//         }

//         public async Task<ApiResponse<StaffInfo>> Handle(
//             StaffCreateCommand request,
//             CancellationToken cancellationToken)
//         {
//             var phoneExists = await _context.Staffs
//                 .AnyAsync(s => s.PhoneNumber == request.PhoneNumber && !s.IsDeleted, cancellationToken);
//             if (phoneExists)
//                 return ApiResponse<StaffInfo>.BadRequest(
//                     $"Phone number '{request.PhoneNumber}' already exists.");

//             var staff = new DomainStaff
//             {
//                 FirstName = request.FirstName.Trim(),
//                 LastName = request.LastName.Trim(),
//                 ImageProfile = request.ImageProfile.Trim(),
//                 PhoneNumber = request.PhoneNumber.Trim(),
//                 Position = request.Position.Trim(),
//                 Salary = request.Salary,
//                 Status = request.Status,
//             };

//             _context.Staffs.Add(staff);
//             await _context.SaveChangesAsync(cancellationToken);
//             var data = new StaffInfo
//             {
//                 Id = staff.Id,
//                 FirstName = staff.FirstName,
//                 LastName = staff.LastName,
//                 ImageProfile = staff.ImageProfile,
//                 PhoneNumber = staff.PhoneNumber,
//                 Position = staff.Position,
//                 Salary = staff.Salary,
//                 Status = staff.Status,
//                 IsDeleted = staff.IsDeleted,
//                 CreatedDate = staff.CreatedDate,
//                 CreatedBy = staff.CreatedBy,
//                 User = null
//             };

//             return ApiResponse<StaffInfo>.Created(data, "Staff created successfully.");
//         }
//     }
// }

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using DomainStaff = POS.Domain.Entities.Staff;

namespace POS.Application.Features.Staff
{
    public record StaffCreateCommand : IRequest<ApiResponse<StaffInfo>>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ImageProfile { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public bool Status { get; set; } = true;
        public int? SupervisorId { get; set; }
    }

    public class StaffCreateCommandValidator : AbstractValidator<StaffCreateCommand>
    {
        public StaffCreateCommandValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");
            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.");
            RuleFor(x => x.Position)
                .NotEmpty().WithMessage("Position is required.")
                .MaximumLength(100).WithMessage("Position must not exceed 100 characters.");
            RuleFor(x => x.Salary)
                .GreaterThanOrEqualTo(0).WithMessage("Salary must be 0 or greater.");
        }
    }

    public class StaffCreateCommandHandler : IRequestHandler<StaffCreateCommand, ApiResponse<StaffInfo>>
    {
        private readonly IMyAppDbContext _context;

        public StaffCreateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<StaffInfo>> Handle(
            StaffCreateCommand request,
            CancellationToken cancellationToken)
        {
            var phoneExists = await _context.Staffs
                .AnyAsync(s => s.PhoneNumber == request.PhoneNumber && !s.IsDeleted, cancellationToken);
            if (phoneExists)
                return ApiResponse<StaffInfo>.BadRequest(
                    $"Phone number '{request.PhoneNumber}' already exists.");

            if (request.SupervisorId.HasValue)
            {
                // ✅ Validate supervisor exists
                var supervisorExists = await _context.Staffs
                    .AnyAsync(s => s.Id == request.SupervisorId.Value && !s.IsDeleted, cancellationToken);
                if (!supervisorExists)
                    return ApiResponse<StaffInfo>.BadRequest(
                        $"Supervisor with ID {request.SupervisorId.Value} was not found.");

                // ✅ For create — new staff has no ID yet
                // But validate supervisor chain is not already circular
                var visited = new HashSet<int>();
                var currentId = (int?)request.SupervisorId.Value;
                var isCircular = false;

                while (currentId.HasValue)
                {
                    if (visited.Contains(currentId.Value))
                    {
                        isCircular = true;
                        break;
                    }

                    visited.Add(currentId.Value);

                    currentId = await _context.Staffs
                        .AsNoTracking()
                        .Where(s => s.Id == currentId.Value && !s.IsDeleted)
                        .Select(s => s.SupervisorId)
                        .FirstOrDefaultAsync(cancellationToken);
                }

                if (isCircular)
                    return ApiResponse<StaffInfo>.BadRequest(
                        "The selected supervisor is part of a circular chain. Please select a valid supervisor.");
            }

            var staff = new DomainStaff
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                ImageProfile = request.ImageProfile.Trim(),
                PhoneNumber = request.PhoneNumber.Trim(),
                Position = request.Position.Trim(),
                Salary = request.Salary,
                Status = request.Status,
                SupervisorId = request.SupervisorId,
            };

            _context.Staffs.Add(staff);
            await _context.SaveChangesAsync(cancellationToken);

            var supervisor = request.SupervisorId.HasValue
                ? await _context.Staffs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == request.SupervisorId.Value && !s.IsDeleted, cancellationToken)
                : null;

            var data = new StaffInfo
            {
                Id = staff.Id,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                ImageProfile = staff.ImageProfile,
                PhoneNumber = staff.PhoneNumber,
                Position = staff.Position,
                Salary = staff.Salary,
                Status = staff.Status,
                IsDeleted = staff.IsDeleted,
                SupervisorId = staff.SupervisorId,
                Supervisor = supervisor != null ? new SupervisorInfo
                {
                    Id = supervisor.Id,
                    FullName = supervisor.FirstName + " " + supervisor.LastName,
                    Position = supervisor.Position,
                    ImageProfile = supervisor.ImageProfile,
                } : null,
                CreatedDate = staff.CreatedDate,
                CreatedBy = staff.CreatedBy,
                User = null
            };

            return ApiResponse<StaffInfo>.Created(data, "Staff created successfully.");
        }
    }
}