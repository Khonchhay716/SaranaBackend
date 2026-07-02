using Microsoft.AspNetCore.Authentication;
using POS.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace POS.Infrastructure.Services
{
    public class PermissionClaimsTransformation : IClaimsTransformation
    {
        private readonly IMyAppDbContext _context;

        public PermissionClaimsTransformation(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var identity = principal.Identity as ClaimsIdentity;
            if (identity == null || !identity.IsAuthenticated) return principal;

            // Prevent infinite loop: if we already added permissions, skip
            if (identity.HasClaim(c => c.Type == "permission")) return principal;

            var userIdClaim = identity.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId)) return principal;

            var permissions = await _context.Persons
                .Where(p => p.Id == userId && !p.IsDeleted)
                .SelectMany(p => p.PersonRoles) // get role all in user
                .Where(pr => !pr.Role.IsDeleted)
                .SelectMany(pr => pr.Role.RolePermissions) // get all permission in all role 
                .Select(rp => rp.PermissionName)
                .Distinct()//remove permission dulicate 
                .ToListAsync();

            foreach (var permission in permissions)
            {
                identity.AddClaim(new Claim("permission", permission));
            }

            return principal;
        }
    }
}