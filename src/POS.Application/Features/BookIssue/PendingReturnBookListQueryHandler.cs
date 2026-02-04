// POS.Application/Features/BookIssue/GetPendingReturnBooksQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.BookIssue
{
    public class GetPendingReturnBooksQuery : PaginationRequest, IRequest<PaginatedResult<BookIssueInfo>>
    {
        public string? Search { get; set; }
        public bool? IncludeOverdueOnly { get; set; }
    }

    public class GetPendingReturnBooksQueryHandler : IRequestHandler<GetPendingReturnBooksQuery, PaginatedResult<BookIssueInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetPendingReturnBooksQueryHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<PaginatedResult<BookIssueInfo>> Handle(GetPendingReturnBooksQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            var query = _context.BookIssues
                .Include(bi => bi.Book)
                .Include(bi => bi.LibraryMember)
                    .ThenInclude(m => m.Person)
                .Where(bi => !bi.IsDeleted)
                .Where(bi => bi.LibraryMember.PersonId == userId.Value)
                .Where(bi => bi.ReturnDate == null) // Only books not yet returned
                .Where(bi => bi.Status == "Issued" || bi.Status == "Renew") // Pending statuses
                .AsNoTracking();

            // Search filter
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(bi =>
                    bi.Book.Title.Contains(request.Search) ||
                    bi.Book.Author.Contains(request.Search)
                );
            }

            // Overdue only filter
            if (request.IncludeOverdueOnly.HasValue && request.IncludeOverdueOnly.Value)
            {
                var now = DateTimeOffset.UtcNow;
                query = query.Where(bi => bi.DueDate < now);
            }

            // Order by due date (earliest first)
            query = query.OrderBy(bi => bi.DueDate);

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