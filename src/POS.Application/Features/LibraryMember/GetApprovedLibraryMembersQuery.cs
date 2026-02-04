// POS.Application/Features/LibraryMember/GetApprovedLibraryMembersQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.LibraryMember
{
    public class GetApprovedLibraryMembersQuery : PaginationRequest, IRequest<PaginatedResult<LibraryMemberInfo>>
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
    }

    public class GetApprovedLibraryMembersQueryHandler : IRequestHandler<GetApprovedLibraryMembersQuery, PaginatedResult<LibraryMemberInfo>>
    {
        private readonly IMyAppDbContext _context;

        public GetApprovedLibraryMembersQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<LibraryMemberInfo>> Handle(GetApprovedLibraryMembersQuery request, CancellationToken cancellationToken)
        {
            var query = _context.LibraryMembers
                .Include(m => m.Person)
                .Where(m => !m.IsDeleted && m.Status == (int)LibraryMemberStatus.Approved)
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

            // Order by most recent first
            query = query.OrderByDescending(m => m.CreatedDate);

            // Project to LibraryMemberInfo
            var projectedQuery = query.Select(m => new LibraryMemberInfo
            {
                Id = m.Id,
                PersonId = m.PersonId,
                PersonName = m.Person.FirstName + " " + m.Person.LastName,
                MembershipNo = m.MembershipNo,
                MembershipType = m.MembershipType,
                Email = m.Email,
                IsActive = m.IsActive,
                MaxBooksAllowed = m.MaxBooksAllowed,
                Address = m.Address,
                PhoneNumber = m.PhoneNumber,
                Status = (LibraryMemberStatus)m.Status
            });

            return await projectedQuery.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}