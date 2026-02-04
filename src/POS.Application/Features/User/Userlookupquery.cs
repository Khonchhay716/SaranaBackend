// POS.Application/Features/User/UserLookupQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.User
{
    public class UserLookupQuery : IRequest<ApiResponse<List<UserLookupInfo>>>
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public int? RoleId { get; set; } // Optional: filter by specific role
    }

    public class UserLookupInfo
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? ImageProfile { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserLookupQueryHandler : IRequestHandler<UserLookupQuery, ApiResponse<List<UserLookupInfo>>>
    {
        private readonly IMyAppDbContext _context;

        public UserLookupQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<UserLookupInfo>>> Handle(UserLookupQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Persons
                .AsNoTracking()
                .Include(p => p.PersonRoles)
                .Where(p => !p.IsDeleted);

            // Search filter
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(p =>
                    p.Username.Contains(request.Search) ||
                    p.Email.Contains(request.Search) ||
                    p.FirstName.Contains(request.Search) ||
                    p.LastName.Contains(request.Search) ||
                    (p.PhoneNumber != null && p.PhoneNumber.Contains(request.Search))
                );
            }

            // Active status filter
            if (request.IsActive.HasValue)
            {
                query = query.Where(p => p.IsActive == request.IsActive.Value);
            }

            // Role filter
            if (request.RoleId.HasValue)
            {
                query = query.Where(p => p.PersonRoles.Any(pr => pr.RoleId == request.RoleId.Value && !pr.Role.IsDeleted));
            }

            // Order by first name, then last name
            query = query.OrderBy(p => p.FirstName).ThenBy(p => p.LastName);

            // Project to lookup info
            var result = await query
                .Select(p => new UserLookupInfo
                {
                    Id = p.Id,
                    Username = p.Username,
                })
                .ToListAsync(cancellationToken);

            return ApiResponse<List<UserLookupInfo>>.Ok(result);
        }
    }
}