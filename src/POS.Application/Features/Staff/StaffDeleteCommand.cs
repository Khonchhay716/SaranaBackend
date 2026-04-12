// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Dto;
// using POS.Application.Common.Interfaces;

// namespace POS.Application.Features.Staff
// {
//     public record StaffDeleteCommand(int Id) : IRequest<ApiResponse<bool>>;

//     public class StaffDeleteCommandHandler : IRequestHandler<StaffDeleteCommand, ApiResponse<bool>>
//     {
//         private readonly IMyAppDbContext _context;

//         public StaffDeleteCommandHandler(IMyAppDbContext context)
//         {
//             _context = context;
//         }

//         public async Task<ApiResponse<bool>> Handle(
//             StaffDeleteCommand request,
//             CancellationToken  cancellationToken)
//         {
//             var staff = await _context.Staffs
//                 .Include(s => s.Person)
//                 .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);

//             if (staff == null)
//                 return ApiResponse<bool>.NotFound(
//                     $"Staff with ID {request.Id} was not found.");

//             if (staff.Person != null && !staff.Person.IsDeleted)
//                 return ApiResponse<bool>.BadRequest(
//                     $"Cannot delete Staff with ID {request.Id} because it is linked to user '{staff.Person.Username}'. Please unlink the user account first.");

//             staff.IsDeleted   = true;
//             staff.DeletedDate = DateTimeOffset.UtcNow;

//             await _context.SaveChangesAsync(cancellationToken);

//             return ApiResponse<bool>.Ok(true, "Staff deleted successfully.");
//         }
//     }
// }






using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Staff
{
    public record StaffDeleteCommand(int Id) : IRequest<ApiResponse<bool>>;

    public class StaffDeleteCommandHandler : IRequestHandler<StaffDeleteCommand, ApiResponse<bool>>
    {
        private readonly IMyAppDbContext _context;

        public StaffDeleteCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<bool>> Handle(
            StaffDeleteCommand request,
            CancellationToken cancellationToken)
        {
            var staff = await _context.Staffs
                .Include(s => s.Person)
                .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);

            if (staff == null)
                return ApiResponse<bool>.NotFound(
                    $"Staff with ID {request.Id} was not found.");

            if (staff.Person != null && !staff.Person.IsDeleted)
                return ApiResponse<bool>.BadRequest(
                    $"Cannot delete Staff with ID {request.Id} because it is linked to user '{staff.Person.Username}'. Please unlink the user account first.");

            // Check if this staff is supervisor of others
            var hasSupervisees = await _context.Staffs
                .AnyAsync(s => s.SupervisorId == request.Id && !s.IsDeleted, cancellationToken);
            if (hasSupervisees)
                return ApiResponse<bool>.BadRequest(
                    $"Cannot delete Staff with ID {request.Id} because they are assigned as a supervisor for other staff members.");

            staff.IsDeleted   = true;
            staff.DeletedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.Ok(true, "Staff deleted successfully.");
        }
    }
}