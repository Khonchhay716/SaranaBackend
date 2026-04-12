// using FluentValidation;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Dto;
// using POS.Application.Common.Extensions;
// using POS.Application.Common.Interfaces;
// using POS.Application.Common.Typebase;

// namespace POS.Application.Features.Staff
// {
//     public class StaffQuery : IRequest<ApiResponse<StaffInfo>>
//     {
//         public int Id { get; set; }
//     }

//     public class StaffQueryHandler : IRequestHandler<StaffQuery, ApiResponse<StaffInfo>>
//     {
//         private readonly IMyAppDbContext _context;

//         public StaffQueryHandler(IMyAppDbContext context)
//         {
//             _context = context;
//         }

//         public async Task<ApiResponse<StaffInfo>> Handle(
//             StaffQuery request,
//             CancellationToken cancellationToken)
//         {
//             var staff = await _context.Staffs
//                 .AsNoTracking()
//                 .Include(s => s.Person)
//                 .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);

//             if (staff == null)
//                 return ApiResponse<StaffInfo>.NotFound(
//                     $"Staff with ID {request.Id} was not found.");

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
//                 UpdatedDate = staff.UpdatedDate,
//                 UpdatedBy = staff.UpdatedBy,
//                 DeletedDate = staff.DeletedDate,
//                 DeletedBy = staff.DeletedBy,
//                 User = staff.Person != null && !staff.Person.IsDeleted
//                     ? new LinkedUserInfo
//                     {
//                         Id = staff.Person.Id,
//                         Username = staff.Person.Username,
//                         Email = staff.Person.Email,
//                         IsActive = staff.Person.IsActive
//                     }
//                     : null
//             };

//             return ApiResponse<StaffInfo>.Ok(data, "Staff retrieved successfully.");
//         }
//     }
// }





using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Staff
{
    public class StaffQuery : IRequest<ApiResponse<StaffInfo>>
    {
        public int Id { get; set; }
    }

    public class StaffQueryHandler : IRequestHandler<StaffQuery, ApiResponse<StaffInfo>>
    {
        private readonly IMyAppDbContext _context;

        public StaffQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<StaffInfo>> Handle(
            StaffQuery request,
            CancellationToken cancellationToken)
        {
            var staff = await _context.Staffs
                .AsNoTracking()
                .Include(s => s.Person)
                .Include(s => s.Supervisor) // ✅ Load Supervisor
                .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);

            if (staff == null)
                return ApiResponse<StaffInfo>.NotFound(
                    $"Staff with ID {request.Id} was not found.");

            var data = new StaffInfo
            {
                Id           = staff.Id,
                FirstName    = staff.FirstName,
                LastName     = staff.LastName,
                ImageProfile = staff.ImageProfile,
                PhoneNumber  = staff.PhoneNumber,
                Position     = staff.Position,
                Salary       = staff.Salary,
                Status       = staff.Status,
                IsDeleted    = staff.IsDeleted,
                SupervisorId = staff.SupervisorId,
                Supervisor   = staff.Supervisor != null && !staff.Supervisor.IsDeleted
                    ? new SupervisorInfo
                    {
                        Id           = staff.Supervisor.Id,
                        FullName     = staff.Supervisor.FirstName + " " + staff.Supervisor.LastName,
                        Position     = staff.Supervisor.Position,
                        ImageProfile = staff.Supervisor.ImageProfile,
                    } : null,
                CreatedDate  = staff.CreatedDate,
                CreatedBy    = staff.CreatedBy,
                UpdatedDate  = staff.UpdatedDate,
                UpdatedBy    = staff.UpdatedBy,
                DeletedDate  = staff.DeletedDate,
                DeletedBy    = staff.DeletedBy,
                User = staff.Person != null && !staff.Person.IsDeleted
                    ? new LinkedUserInfo
                    {
                        Id       = staff.Person.Id,
                        Username = staff.Person.Username,
                        Email    = staff.Person.Email,
                        IsActive = staff.Person.IsActive
                    }
                    : null
            };

            return ApiResponse<StaffInfo>.Ok(data, "Staff retrieved successfully.");
        }
    }
}