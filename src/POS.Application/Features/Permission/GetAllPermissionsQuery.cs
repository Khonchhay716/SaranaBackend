using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Permission
{
    public record GetAllPermissionsQuery : IRequest<List<PermissionInfo>>;

    public class GetAllPermissionsQueryHandler : IRequestHandler<GetAllPermissionsQuery, List<PermissionInfo>>
    {
        public Task<List<PermissionInfo>> Handle(GetAllPermissionsQuery request, CancellationToken cancellationToken)
        {
            var permissions = PermissionData.Permissions
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .Select((p, index) => new PermissionInfo
                {
                    Id = index + 1,
                    Name = p.Name,
                    Description = p.Description,
                    Category = p.Category,
                })
                .ToList();

            return Task.FromResult(permissions);
        }
    }
}