// POS.Application/Features/Role/GetRoleByIdQuery.cs

using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Role
{
    // FIX: Make the return type nullable
    public record GetRoleByIdQuery(int RoleId) : IRequest<RoleInfo?>;

    public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, RoleInfo?>
    {
        private readonly IMyAppDbContext _context;

        public GetRoleByIdQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<RoleInfo?> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
        {
            var role = await _context.Roles
                .AsNoTracking()
                .Where(r => r.Id == request.RoleId && !r.IsDeleted)
                .Select(r => new RoleInfo
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Permissions = r.RolePermissions.Select(rp => rp.PermissionName).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            // Returning null is now valid with the nullable return type
            return role;
        }
    }
}