using POS.Application.Common.Dto;

// POS.Application/Features/User/GetUserByIdQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            var user = await _context.Persons
                .AsNoTracking()
                .Where(p => p.Id == request.UserId && !p.IsDeleted)
                .Select(p => new UserDetailInfo
                {
                    Id = p.Id,
                    Username = p.Username,
                    ImageProfile= p.ImageProfile,
                    Email = p.Email,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    PhoneNumber = p.PhoneNumber,
                    IsActive = p.IsActive,
                    CreatedDate = p.CreatedDate,
                    Roles = p.PersonRoles.Select(pr => new RoleBasicInfo
                    {
                        Id = pr.Role.Id,
                        Name = pr.Role.Name,
                        Description = pr.Role.Description
                    }).ToList(),
                    Permissions = p.PersonRoles
                        .SelectMany(pr => pr.Role.RolePermissions)
                        .Select(rp => rp.PermissionName)
                        .Distinct()
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                return ApiResponse<UserDetailInfo>.NotFound("User not found");
            }

            return ApiResponse<UserDetailInfo>.Ok(user);
        }
    }
}