// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Caching.Memory;
// using POS.Application.Common.Interfaces;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Security.Claims;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.Http;
// namespace POS.Application.Common.Services
// {
//     public class CurrentUserService : ICurrentUserService
//     {
//         private readonly IHttpContextAccessor _httpContextAccessor;
//         private readonly IMyAppDbContext _context;
//         private readonly IMemoryCache _cache;

//         public CurrentUserService(IHttpContextAccessor httpContextAccessor, IMyAppDbContext context, IMemoryCache cache)
//         {
//             _httpContextAccessor = httpContextAccessor;
//             _context = context;
//             _cache = cache;
//         }

//         public int? UserId
//         {
//             get
//             {
//                 var claim = _httpContextAccessor.HttpContext?.User?.Claims
//                                 .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
//                 if (int.TryParse(claim, out var id))
//                     return id;
//                 return null;
//             }
//         }

//         public async Task<IEnumerable<string>> GetPermissionsAsync()
//         {
//             if (UserId == null) return Enumerable.Empty<string>();

//             var cacheKey = $"user-permissions-{UserId}";

//             if (_cache.TryGetValue(cacheKey, out List<string> cachedPermissions))
//             {
//                 return cachedPermissions;
//             }

//             var permissions = await _context.Persons
//                 .AsNoTracking()
//                 .Where(p => p.Id == UserId.Value)
//                 .SelectMany(p => p.PersonRoles)  
//                 .Where(pr => !pr.Role.IsDeleted)
//                 .SelectMany(pr => pr.Role.RolePermissions)
//                 .Select(rp => rp.PermissionName)  
//                 .Distinct()
//                 .ToListAsync();

//             var cacheEntryOptions = new MemoryCacheEntryOptions()
//                 .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

//             _cache.Set(cacheKey, permissions, cacheEntryOptions);

//             return permissions;
//         }
//     }
// }




using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using POS.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace POS.Application.Common.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMyAppDbContext _context;
        private readonly IMemoryCache _cache;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, IMyAppDbContext context, IMemoryCache cache)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _cache = cache;
        }

        public int? UserId
        {
            get
            {
                var claim = _httpContextAccessor.HttpContext?.User?.Claims
                                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(claim, out var id))
                    return id;
                return null;
            }
        }

        public async Task<IEnumerable<string>> GetPermissionsAsync()
        {
            // ⭐ Return empty if no user is logged in
            if (UserId == null)
                return Enumerable.Empty<string>();

            var cacheKey = $"user-permissions-{UserId}";

            if (_cache.TryGetValue(cacheKey, out List<string> cachedPermissions))
            {
                return cachedPermissions;
            }
            
            var permissions = await _context.Persons
                .AsNoTracking()
                .Where(p => p.Id == UserId.Value)
                .SelectMany(p => p.PersonRoles)  
                .Where(pr => !pr.Role.IsDeleted)
                .SelectMany(pr => pr.Role.RolePermissions)
                .Select(rp => rp.PermissionName)  
                .Distinct()
                .ToListAsync();

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

            _cache.Set(cacheKey, permissions, cacheEntryOptions);

            return permissions;
        }
    }
}