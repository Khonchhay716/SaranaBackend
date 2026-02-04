using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Role
{
    public record GetRoleByIdQuery(int RoleId) : IRequest<ApiResponse<RoleInfo>>;

    public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, ApiResponse<RoleInfo>>
    {
        private readonly IMyAppDbContext _context;

        public GetRoleByIdQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<RoleInfo>> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
        {
            // Project directly to RoleInfo
            var role = await _context.Roles
                .AsNoTracking()
                .Where(r => r.Id == request.RoleId && !r.IsDeleted)
                .Select(r => new RoleInfo
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Permissions = r.RolePermissions
                        // .Where(rp => !rp.IsDeleted)
                        .Select(rp => rp.PermissionName)
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (role == null)
            {
                return ApiResponse<RoleInfo>.NotFound($"Role with id {request.RoleId} not found");
            }

            return ApiResponse<RoleInfo>.Ok(role);
        }
    }
}
