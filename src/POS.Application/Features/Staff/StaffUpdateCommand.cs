// using FluentValidation;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Dto;
// using POS.Application.Common.Interfaces;

// namespace POS.Application.Features.Staff
// {
//     public record StaffUpdateCommand : IRequest<ApiResponse<StaffInfo>>
//     {
//         [System.Text.Json.Serialization.JsonIgnore]
//         public int Id { get; set; }

//         public string  FirstName    { get; set; } = string.Empty;
//         public string  LastName     { get; set; } = string.Empty;
//         public string  ImageProfile { get; set; } = string.Empty;
//         public string  PhoneNumber  { get; set; } = string.Empty;
//         public string  Position     { get; set; } = string.Empty;
//         public decimal Salary       { get; set; }
//         public bool    Status       { get; set; } = true;
//     }

//     public class StaffUpdateCommandValidator : AbstractValidator<StaffUpdateCommand>
//     {
//         public StaffUpdateCommandValidator()
//         {
//             RuleFor(x => x.Id)
//                 .GreaterThan(0).WithMessage("A valid Staff ID is required.");

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

//     public class StaffUpdateCommandHandler : IRequestHandler<StaffUpdateCommand, ApiResponse<StaffInfo>>
//     {
//         private readonly IMyAppDbContext _context;

//         public StaffUpdateCommandHandler(IMyAppDbContext context)
//         {
//             _context = context;
//         }

//         public async Task<ApiResponse<StaffInfo>> Handle(
//             StaffUpdateCommand request,
//             CancellationToken  cancellationToken)
//         {
//             var staff = await _context.Staffs
//                 .Include(s => s.Person)
//                 .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);

//             if (staff == null)
//                 return ApiResponse<StaffInfo>.NotFound(
//                     $"Staff with ID {request.Id} was not found.");

//             var phoneExists = await _context.Staffs
//                 .AnyAsync(s => s.PhoneNumber == request.PhoneNumber
//                             && s.Id != request.Id
//                             && !s.IsDeleted, cancellationToken);
//             if (phoneExists)
//                 return ApiResponse<StaffInfo>.BadRequest(
//                     $"Phone number '{request.PhoneNumber}' is already used by another staff.");

//             staff.FirstName    = request.FirstName.Trim();
//             staff.LastName     = request.LastName.Trim();
//             staff.ImageProfile = request.ImageProfile.Trim();
//             staff.PhoneNumber  = request.PhoneNumber.Trim();
//             staff.Position     = request.Position.Trim();
//             staff.Salary       = request.Salary;
//             staff.Status       = request.Status;
//             staff.UpdatedDate  = DateTimeOffset.UtcNow;

//             await _context.SaveChangesAsync(cancellationToken);
//             var data = new StaffInfo
//             {
//                 Id           = staff.Id,
//                 FirstName    = staff.FirstName,
//                 LastName     = staff.LastName,
//                 ImageProfile = staff.ImageProfile,
//                 PhoneNumber  = staff.PhoneNumber,
//                 Position     = staff.Position,
//                 Salary       = staff.Salary,
//                 Status       = staff.Status,
//                 IsDeleted    = staff.IsDeleted,
//                 CreatedDate  = staff.CreatedDate,
//                 CreatedBy    = staff.CreatedBy,
//                 UpdatedDate  = staff.UpdatedDate,
//                 UpdatedBy    = staff.UpdatedBy,
//                 User = staff.Person != null && !staff.Person.IsDeleted
//                     ? new LinkedUserInfo
//                     {
//                         Id       = staff.Person.Id,
//                         Username = staff.Person.Username,
//                         Email    = staff.Person.Email,
//                         IsActive = staff.Person.IsActive
//                     }
//                     : null
//             };

//             return ApiResponse<StaffInfo>.Ok(data, "Staff updated successfully.");
//         }
//     }
// }



using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Staff
{
    public record StaffUpdateCommand : IRequest<ApiResponse<StaffInfo>>
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ImageProfile { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public bool Status { get; set; } = true;
        public int? SupervisorId { get; set; }
    }

    public class StaffUpdateCommandValidator : AbstractValidator<StaffUpdateCommand>
    {
        public StaffUpdateCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("A valid Staff ID is required.");
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

    public class StaffUpdateCommandHandler : IRequestHandler<StaffUpdateCommand, ApiResponse<StaffInfo>>
    {
        private readonly IMyAppDbContext _context;

        public StaffUpdateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<StaffInfo>> Handle(
            StaffUpdateCommand request,
            CancellationToken cancellationToken)
        {
            var staff = await _context.Staffs
                .Include(s => s.Person)
                .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);

            if (staff == null)
                return ApiResponse<StaffInfo>.NotFound(
                    $"Staff with ID {request.Id} was not found.");

            // ✅ Prevent self-supervisor
            if (request.SupervisorId.HasValue && request.SupervisorId.Value == request.Id)
                return ApiResponse<StaffInfo>.BadRequest(
                    "A staff member cannot be their own supervisor.");

            if (request.SupervisorId.HasValue)
            {
                // ✅ Validate supervisor exists
                var supervisorExists = await _context.Staffs
                    .AnyAsync(s => s.Id == request.SupervisorId.Value && !s.IsDeleted, cancellationToken);
                if (!supervisorExists)
                    return ApiResponse<StaffInfo>.BadRequest(
                        $"Supervisor with ID {request.SupervisorId.Value} was not found.");

                // ✅ Circular chain detection — inline
                var visited = new HashSet<int>();
                var currentId = (int?)request.SupervisorId.Value;
                var isCircular = false;

                while (currentId.HasValue)
                {
                    if (currentId.Value == request.Id)
                    {
                        isCircular = true;
                        break;
                    }
                    if (visited.Contains(currentId.Value))
                        break;

                    visited.Add(currentId.Value);

                    currentId = await _context.Staffs
                        .AsNoTracking()
                        .Where(s => s.Id == currentId.Value && !s.IsDeleted)
                        .Select(s => s.SupervisorId)
                        .FirstOrDefaultAsync(cancellationToken);
                }

                if (isCircular)
                    return ApiResponse<StaffInfo>.BadRequest(
                        "Circular supervisor chain detected. This assignment would create a loop (e.g., A → B → C → A).");
            }

            // ✅ Validate phone
            var phoneExists = await _context.Staffs
                .AnyAsync(s => s.PhoneNumber == request.PhoneNumber
                            && s.Id != request.Id
                            && !s.IsDeleted, cancellationToken);
            if (phoneExists)
                return ApiResponse<StaffInfo>.BadRequest(
                    $"Phone number '{request.PhoneNumber}' is already used by another staff.");

            staff.FirstName = request.FirstName.Trim();
            staff.LastName = request.LastName.Trim();
            staff.ImageProfile = request.ImageProfile.Trim();
            staff.PhoneNumber = request.PhoneNumber.Trim();
            staff.Position = request.Position.Trim();
            staff.Salary = request.Salary;
            staff.Status = request.Status;
            staff.SupervisorId = request.SupervisorId;
            staff.UpdatedDate = DateTimeOffset.UtcNow;

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
                UpdatedDate = staff.UpdatedDate,
                UpdatedBy = staff.UpdatedBy,
                User = staff.Person != null && !staff.Person.IsDeleted
                    ? new LinkedUserInfo
                    {
                        Id = staff.Person.Id,
                        Username = staff.Person.Username,
                        Email = staff.Person.Email,
                        IsActive = staff.Person.IsActive
                    }
                    : null
            };

            return ApiResponse<StaffInfo>.Ok(data, "Staff updated successfully.");
        }
    }
}