using Microsoft.AspNetCore.Authorization;
using POS.Application.Common.Interfaces;
using System.Threading.Tasks;

namespace POS.Application.Common.Authorization
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly ICurrentUserService _currentUserService;

        public PermissionAuthorizationHandler(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var permissions = await _currentUserService.GetPermissionsAsync();

            if (permissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
            }
        }
    }
}