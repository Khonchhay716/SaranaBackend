// POS.Application/Features/Auth/GetCurrentUserQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Features.User;
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
            {
                return ApiResponse<UserDetailInfo>.Unauthorized("User not authenticated");
            }
            var user = await _context.Persons
                .AsNoTracking()
                .Include(p => p.PersonRoles)
                    .ThenInclude(pr => pr.Role)
                        .ThenInclude(r => r.RolePermissions)
                .Where(p => p.Id == userId.Value && !p.IsDeleted)
                .Select(p => new UserDetailInfo
                {
                    Id = p.Id,
                    Username = p.Username,
                    Email = p.Email,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    PhoneNumber = p.PhoneNumber,
                    IsActive = p.IsActive,
                    CreatedDate = p.CreatedDate,
                    Roles = p.PersonRoles
                        .Where(pr => !pr.Role.IsDeleted)
                        .Select(pr => new RoleBasicInfo
                        {
                            Id = pr.Role.Id,
                            Name = pr.Role.Name,
                            Description = pr.Role.Description
                        }).ToList(),
                    Permissions = p.PersonRoles
                        .Where(pr => !pr.Role.IsDeleted)
                        .SelectMany(pr => pr.Role.RolePermissions)
                        .Select(rp => rp.PermissionName)  // ← Use PermissionName directly
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