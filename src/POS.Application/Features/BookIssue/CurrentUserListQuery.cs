// POS.Application/Features/BookIssue/GetCurrentUserBookIssuesQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.BookIssue
{
    public class GetCurrentUserBookIssuesQuery : PaginationRequest, IRequest<PaginatedResult<BookIssueInfo>>
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public int? BookId { get; set; }
        public bool? IsOverdue { get; set; }
    }

    public class GetCurrentUserBookIssuesQueryHandler : IRequestHandler<GetCurrentUserBookIssuesQuery, PaginatedResult<BookIssueInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetCurrentUserBookIssuesQueryHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<PaginatedResult<BookIssueInfo>> Handle(GetCurrentUserBookIssuesQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            var query = _context.BookIssues
                .Include(bi => bi.Book)
                .Include(bi => bi.LibraryMember)
                    .ThenInclude(m => m.Person)
                .Where(bi => !bi.IsDeleted)
                .Where(bi => bi.LibraryMember.PersonId == userId.Value) // Filter by current user only
                .AsNoTracking();

            // Search filter
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(bi =>
                    bi.Book.Title.Contains(request.Search) ||
                    bi.Book.Author.Contains(request.Search) ||
                    bi.LibraryMember.MembershipNo.Contains(request.Search) ||
                    bi.LibraryMember.Person.FirstName.Contains(request.Search) ||
                    bi.LibraryMember.Person.LastName.Contains(request.Search)
                );
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(bi => bi.Status == request.Status);
            }

            // Book filter
            if (request.BookId.HasValue)
            {
                query = query.Where(bi => bi.BookId == request.BookId.Value);
            }

            // Overdue filter
            if (request.IsOverdue.HasValue)
            {
                var now = DateTimeOffset.UtcNow;
                if (request.IsOverdue.Value)
                {
                    query = query.Where(bi => bi.ReturnDate == null && bi.DueDate < now);
                }
                else
                {
                    query = query.Where(bi => bi.ReturnDate != null || bi.DueDate >= now);
                }
            }

            // Order by most recent first
            query = query.OrderByDescending(bi => bi.IssueDate);

            // Project to BookIssueInfo
            var projectedQuery = query.Select(bi => new BookIssueInfo
            {
                Id = bi.Id,
                BookId = bi.Book.Id,
                BookTitle = bi.Book.Title,
                BookAuthor = bi.Book.Author,
                LibraryMemberId = bi.LibraryMember.Id,
                MemberName = bi.LibraryMember.Person.FirstName + " " + bi.LibraryMember.Person.LastName,
                MembershipNo = bi.LibraryMember.MembershipNo,
                IssueDate = bi.IssueDate,
                DueDate = bi.DueDate,
                ReturnDate = bi.ReturnDate,
                Status = bi.Status,
                Notes = bi.Notes
            });

            return await projectedQuery.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}