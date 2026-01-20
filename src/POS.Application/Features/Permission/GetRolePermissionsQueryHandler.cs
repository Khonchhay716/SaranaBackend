// POS.Application/Features/Permission/GetRolePermissionsQueryHandler.cs

using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Interfaces;
using POS.Application.Features.Role;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Permission
{
    // FIX: Make the return type nullable
    public record GetRolePermissionsQuery(int RoleId) : IRequest<RolePermissionResponse?>;

    public class GetRolePermissionsQueryHandler : IRequestHandler<GetRolePermissionsQuery, RolePermissionResponse?>
    {
        private readonly IMyAppDbContext _context;

        public GetRolePermissionsQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<RolePermissionResponse?> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
        {
            var role = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == request.RoleId && !r.IsDeleted, cancellationToken);

            if (role == null)
            {
                // Returning null is now valid with the nullable return type
                return null;
            }

            var assignedPermissions = await _context.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionName)
                .ToListAsync(cancellationToken);

            var groupedPermissions = PermissionData.Permissions
                .GroupBy(p => p.Category)
                .Select(g => new PermissionGroupDto
                {
                    Group = g.Key,
                    Permissions = g.Select(p => new PermissionItemDto
                    {
                        Name = p.Name,
                        Description = p.Description,
                        IsAssigned = assignedPermissions.Contains(p.Name)
                    }).ToList()
                })
                .OrderBy(g => g.Group)
                .ToList();

            var response = new RolePermissionResponse
            {
                RoleId = role.Id,
                RoleName = role.Name,
                RoleDescription = role.Description,
                AssignedPermissions = assignedPermissions,
                AllPermissions = groupedPermissions
            };

            return response;
        }
    }
}