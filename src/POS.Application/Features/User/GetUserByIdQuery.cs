// using POS.Application.Common.Dto;

// // POS.Application/Features/User/GetUserByIdQuery.cs
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Dto;
// using POS.Application.Common.Interfaces;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;

// namespace POS.Application.Features.User
// {
//     public record GetUserByIdQuery(int UserId) : IRequest<ApiResponse<UserDetailInfo>>;

//     public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, ApiResponse<UserDetailInfo>>
//     {
//         private readonly IMyAppDbContext _context;

//         public GetUserByIdQueryHandler(IMyAppDbContext context)
//         {
//             _context = context;
//         }

//         public async Task<ApiResponse<UserDetailInfo>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
//         {
//             var user = await _context.Persons
//                 .AsNoTracking()
//                 .Where(p => p.Id == request.UserId && !p.IsDeleted)
//                 .Select(p => new UserDetailInfo
//                 {
//                     Id = p.Id,
//                     Username = p.Username,
//                     ImageProfile= p.ImageProfile,
//                     Email = p.Email,
//                     FirstName = p.FirstName,
//                     LastName = p.LastName,
//                     PhoneNumber = p.PhoneNumber,
//                     IsActive = p.IsActive,
//                     CreatedDate = p.CreatedDate,
//                     Roles = p.PersonRoles.Select(pr => new RoleBasicInfo
//                     {
//                         Id = pr.Role.Id,
//                         Name = pr.Role.Name,
//                         Description = pr.Role.Description
//                     }).ToList(),
//                     Permissions = p.PersonRoles
//                         .SelectMany(pr => pr.Role.RolePermissions)
//                         .Select(rp => rp.PermissionName)
//                         .Distinct()
//                         .ToList()
//                 })
//                 .FirstOrDefaultAsync(cancellationToken);

//             if (user == null)
//             {
//                 return ApiResponse<UserDetailInfo>.NotFound("User not found");
//             }

//             return ApiResponse<UserDetailInfo>.Ok(user);
//         }
//     }
// }
// POS.Application/Features/User/GetUserByIdQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;

namespace POS.Application.Features.User
{
    public record GetUserByIdQuery(int UserId) : IRequest<ApiResponse<UserDetailInfo>>;

    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, ApiResponse<UserDetailInfo>>
    {
        private readonly IMyAppDbContext _context;

        public GetUserByIdQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<UserDetailInfo>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            // ✅ Include Staff + Customer ដើម្បីទាញ Info
            var person = await _context.Persons
                .AsNoTracking()
                .Include(p => p.Staff)
                .Include(p => p.Customer)
                .Include(p => p.PersonRoles)
                    .ThenInclude(pr => pr.Role)
                        .ThenInclude(r => r.RolePermissions)
                .FirstOrDefaultAsync(p => p.Id == request.UserId && !p.IsDeleted, cancellationToken);

            if (person == null)
                return ApiResponse<UserDetailInfo>.NotFound("User not found");

            var response = new UserDetailInfo
            {
                Id = person.Id,
                Username = person.Username,
                Email = person.Email,
                IsActive = person.IsActive,
                Type = person.Type.ToString(),
                CreatedDate = person.CreatedDate,
                Roles = person.PersonRoles
                                .Where(pr => !pr.Role.IsDeleted)
                                .Select(pr => new RoleBasicInfo
                                {
                                    Id = pr.Role.Id,
                                    Name = pr.Role.Name,
                                    Description = pr.Role.Description
                                }).ToList(),
                Permissions = person.PersonRoles
                                .Where(pr => !pr.Role.IsDeleted)
                                .SelectMany(pr => pr.Role.RolePermissions)
                                .Select(rp => rp.PermissionName)
                                .Distinct()
                                .ToList(),

                Staff = person.Type == PersonType.Staff && person.Staff != null
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
                    : null,
                    
                Customer = person.Type == PersonType.Customer && person.Customer != null
                    ? new CustomerInfo
                    {
                        Id = person.Customer.Id,
                        FirstName = person.Customer.FirstName,
                        LastName = person.Customer.LastName,
                        PhoneNumber = person.Customer.PhoneNumber,
                        TotalPoint = person.Customer.TotalPoint,
                        ImageProfile = person.Customer.ImageProfile
                        
                    }
                    : null
            };

            return ApiResponse<UserDetailInfo>.Ok(response);
        }
    }
}