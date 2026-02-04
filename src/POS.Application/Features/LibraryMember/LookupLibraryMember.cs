// POS.Application/Features/LibraryMember/LibraryMemberLookupQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.LibraryMember
{
    public class LibraryMemberLookupQuery : IRequest<ApiResponse<List<LibraryMemberLookupInfo>>>
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public LibraryMemberStatus? Status { get; set; }
    }

    public class LibraryMemberLookupInfo
    {
        public int Id { get; set; }
        public string MembershipNo { get; set; } = string.Empty;
        public string PersonName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public LibraryMemberStatus Status { get; set; }
    }

    public class LibraryMemberLookupQueryHandler : IRequestHandler<LibraryMemberLookupQuery, ApiResponse<List<LibraryMemberLookupInfo>>>
    {
        private readonly IMyAppDbContext _context;

        public LibraryMemberLookupQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<LibraryMemberLookupInfo>>> Handle(LibraryMemberLookupQuery request, CancellationToken cancellationToken)
        {
            var query = _context.LibraryMembers
                .Include(m => m.Person)
                .Where(m => !m.IsDeleted)
                .AsNoTracking();

            // Search filter
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(m =>
                    m.MembershipNo.Contains(request.Search) ||
                    m.Email.Contains(request.Search) ||
                    m.Person.FirstName.Contains(request.Search) ||
                    m.Person.LastName.Contains(request.Search) ||
                    (m.PhoneNumber != null && m.PhoneNumber.Contains(request.Search))
                );
            }

            // Active status filter
            if (request.IsActive.HasValue)
            {
                query = query.Where(m => m.IsActive == request.IsActive.Value);
            }

            // Status filter
            if (request.Status.HasValue)
            {
                query = query.Where(m => m.Status == (int)request.Status.Value);
            }

            // Order by person name
            query = query.OrderBy(m => m.Person.FirstName).ThenBy(m => m.Person.LastName);

            // Project to lookup info
            var result = await query
                .Select(m => new LibraryMemberLookupInfo
                {
                    Id = m.Id,
                    MembershipNo = m.MembershipNo,
                    PersonName = m.Person.FirstName + " " + m.Person.LastName,
                    Email = m.Email,
                    IsActive = m.IsActive,
                    Status = (LibraryMemberStatus)m.Status
                })
                .ToListAsync(cancellationToken);

            return ApiResponse<List<LibraryMemberLookupInfo>>.Ok(result);
        }
    }
}