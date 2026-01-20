// POS.Application/Commons/Services/CurrentUserService.cs
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using POS.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace POS.Infrastructure.Common.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMyAppDbContext _context;
        private readonly IMemoryCache _cache;

        public CurrentUserService(
            IHttpContextAccessor httpContextAccessor, 
            IMyAppDbContext context, 
            IMemoryCache cache)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _cache = cache;
        }

        public int? UserId
        {
            get
            {
                var userIdStr = _httpContextAccessor.HttpContext?.User?.Claims
                    .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                return int.TryParse(userIdStr, out var id) ? id : (int?)null;
            }
        }

        public async Task<IEnumerable<string>> GetPermissionsAsync()
        {
            var userId = UserId;
            if (userId == null) 
            {
                return Enumerable.Empty<string>();
            }

            var cacheKey = $"user-permissions-{userId}";

            if (_cache.TryGetValue(cacheKey, out List<string>? cachedPermissions) && cachedPermissions != null)
                return cachedPermissions;

            // Query through Persons and navigate to PersonRoles collection
            var permissions = await _context.Persons
                .AsNoTracking()
                .Where(p => p.Id == userId.Value)
                .SelectMany(p => p.PersonRoles) 
                .Where(pr => !pr.Role.IsDeleted)
                .SelectMany(pr => pr.Role.RolePermissions)
                .Select(rp => rp.PermissionName)
                .Distinct()
                .ToListAsync();

            _cache.Set(cacheKey, permissions, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

            return permissions;
        }
    }
}