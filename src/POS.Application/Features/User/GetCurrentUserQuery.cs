// // // POS.Application/Features/Auth/GetCurrentUserQuery.cs
// // using MediatR;
// // using Microsoft.EntityFrameworkCore;
// // using POS.Application.Common.Dto;
// // using POS.Application.Common.Interfaces;
// // using POS.Application.Features.User;
// // using System.Linq;
// // using System.Threading;
// // using System.Threading.Tasks;

// // namespace POS.Application.Features.Auth
// // {
// //     public record GetCurrentUserQuery : IRequest<ApiResponse<UserDetailInfo>>;

// //     public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, ApiResponse<UserDetailInfo>>
// //     {
// //         private readonly IMyAppDbContext _context;
// //         private readonly ICurrentUserService _currentUserService;

// //         public GetCurrentUserQueryHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
// //         {
// //             _context = context;
// //             _currentUserService = currentUserService;
// //         }

// //         public async Task<ApiResponse<UserDetailInfo>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
// //         {
// //             var userId = _currentUserService.UserId;

// //             if (userId == null)
// //             {
// //                 return ApiResponse<UserDetailInfo>.Unauthorized("User not authenticated");
// //             }
// //             var user = await _context.Persons
// //                 .AsNoTracking()
// //                 .Include(p => p.PersonRoles)
// //                     .ThenInclude(pr => pr.Role)
// //                         .ThenInclude(r => r.RolePermissions)
// //                 .Where(p => p.Id == userId.Value && !p.IsDeleted)
// //                 .Select(p => new UserDetailInfo
// //                 {
// //                     Id = p.Id,
// //                     Username = p.Username,
// //                     Email = p.Email,
// //                     FirstName = p.FirstName,
// //                     LastName = p.LastName,
// //                     PhoneNumber = p.PhoneNumber,
// //                     IsActive = p.IsActive,
// //                     CreatedDate = p.CreatedDate,
// //                     Roles = p.PersonRoles
// //                         .Where(pr => !pr.Role.IsDeleted)
// //                         .Select(pr => new RoleBasicInfo
// //                         {
// //                             Id = pr.Role.Id,
// //                             Name = pr.Role.Name,
// //                             Description = pr.Role.Description
// //                         }).ToList(),
// //                     Permissions = p.PersonRoles
// //                         .Where(pr => !pr.Role.IsDeleted)
// //                         .SelectMany(pr => pr.Role.RolePermissions)
// //                         .Select(rp => rp.PermissionName)  // ← Use PermissionName directly
// //                         .Distinct()
// //                         .ToList()
// //                 })
// //                 .FirstOrDefaultAsync(cancellationToken);

// //             if (user == null)
// //             {
// //                 return ApiResponse<UserDetailInfo>.NotFound("User not found");
// //             }

// //             return ApiResponse<UserDetailInfo>.Ok(user);
// //         }
// //     }
// // }



// // POS.Application/Features/Auth/GetCurrentUserQuery.cs
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Dto;
// using POS.Application.Common.Interfaces;
// using POS.Application.Features.User;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;

// namespace POS.Application.Features.Auth
// {
//     public record GetCurrentUserQuery : IRequest<ApiResponse<UserDetailInfo>>;

//     public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, ApiResponse<UserDetailInfo>>
//     {
//         private readonly IMyAppDbContext _context;
//         private readonly ICurrentUserService _currentUserService;

//         public GetCurrentUserQueryHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
//         {
//             _context = context;
//             _currentUserService = currentUserService;
//         }

//         public async Task<ApiResponse<UserDetailInfo>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
//         {
//             var userId = _currentUserService.UserId;
//             if (userId == null)
//                 return ApiResponse<UserDetailInfo>.Unauthorized("User not authenticated");

//             var user = await _context.Persons
//                 .AsNoTracking()
//                 .Include(p => p.Staff)                          // ✅
//                 .Include(p => p.PersonRoles)
//                     .ThenInclude(pr => pr.Role)
//                         .ThenInclude(r => r.RolePermissions)
//                 .Where(p => p.Id == userId.Value && !p.IsDeleted)
//                 .Select(p => new UserDetailInfo
//                 {
//                     Id = p.Id,
//                     Username = p.Username,
//                     Email = p.Email,
//                     IsActive = p.IsActive,
//                     CreatedDate = p.CreatedDate,
//                     // ✅ Read personal info from Staff
//                     FirstName = p.Staff != null ? p.Staff.FirstName : string.Empty,
//                     LastName = p.Staff != null ? p.Staff.LastName : string.Empty,
//                     PhoneNumber = p.Staff != null ? p.Staff.PhoneNumber : null,
//                     ImageProfile = p.Staff != null ? p.Staff.ImageProfile : string.Empty,
//                     Roles = p.PersonRoles
//                         .Where(pr => !pr.Role.IsDeleted)
//                         .Select(pr => new RoleBasicInfo
//                         {
//                             Id = pr.Role.Id,
//                             Name = pr.Role.Name,
//                             Description = pr.Role.Description
//                         }).ToList(),
//                     Permissions = p.PersonRoles
//                         .Where(pr => !pr.Role.IsDeleted)
//                         .SelectMany(pr => pr.Role.RolePermissions)
//                         .Select(rp => rp.PermissionName)
//                         .Distinct()
//                         .ToList()
//                 })
//                 .FirstOrDefaultAsync(cancellationToken);

//             if (user == null)
//                 return ApiResponse<UserDetailInfo>.NotFound("User not found");

//             return ApiResponse<UserDetailInfo>.Ok(user);
//         }
//     }
// }


using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Features.User;
using POS.Domain.Entities;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Auth
{
    public record GetCurrentUserQuery : IRequest<ApiResponse<UserDetailInfo>>;

    public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, ApiResponse<UserDetailInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetCurrentUserQueryHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<UserDetailInfo>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
                return ApiResponse<UserDetailInfo>.Unauthorized("User not authenticated.");

            var person = await _context.Persons
                .AsNoTracking()
                .Include(p => p.Staff)
                .Include(p => p.Customer)
                .Include(p => p.PersonRoles)
                    .ThenInclude(pr => pr.Role)
                        .ThenInclude(r => r.RolePermissions)
                .FirstOrDefaultAsync(p => p.Id == userId.Value && !p.IsDeleted, cancellationToken);

            if (person == null)
                return ApiResponse<UserDetailInfo>.NotFound("User not found.");

            var roles = person.PersonRoles
                .Where(pr => pr.Role != null && !pr.Role.IsDeleted)
                .Select(pr => new RoleBasicInfo
                {
                    Id = pr.Role.Id,
                    Name = pr.Role.Name,
                    Description = pr.Role.Description
                }).ToList();

            var permissions = person.PersonRoles
                .Where(pr => pr.Role != null && !pr.Role.IsDeleted)
                .SelectMany(pr => pr.Role.RolePermissions)
                .Select(rp => rp.PermissionName)
                .Distinct()
                .ToList();

            StaffInfo? staffInfo = person.Type == PersonType.Staff && person.Staff != null
                ? new StaffInfo
                {
                    Id = person.Staff.Id,
                    FirstName = person.Staff.FirstName,
                    LastName = person.Staff.LastName,
                    PhoneNumber = person.Staff.PhoneNumber,
                    Position = person.Staff.Position,
                    Salary = person.Staff.Salary,
                    ImageProfile = person.Staff.ImageProfile
                }
                : null;

            CustomerInfo? customerInfo = person.Type == PersonType.Customer && person.Customer != null
                ? new CustomerInfo
                {
                    Id = person.Customer.Id,
                    FirstName = person.Customer.FirstName,
                    LastName = person.Customer.LastName,
                    PhoneNumber = person.Customer.PhoneNumber,
                    TotalPoint = person.Customer.TotalPoint,
                    ImageProfile = person.Customer.ImageProfile
                }
                : null;

            var userDetail = new UserDetailInfo
            {
                Id = person.Id,
                Username = person.Username,
                Email = person.Email,
                IsActive = person.IsActive,
                Type = person.Type.ToString(),
                CreatedDate = person.CreatedDate,
                Roles = roles,
                Permissions = permissions,
                Staff = staffInfo,
                Customer = customerInfo 
            };

            return ApiResponse<UserDetailInfo>.Ok(userDetail);
        }
    }
}