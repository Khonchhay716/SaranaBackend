// POS.Application/Features/BookIssue/GetAllPendingReturnBooksQuery.cs
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
    public class GetAllPendingReturnBooksQuery : PaginationRequest, IRequest<PaginatedResult<BookIssueInfo>>
    {
        public string? Search { get; set; }
        public bool? IncludeOverdueOnly { get; set; }
        public int? LibraryMemberId { get; set; }
    }

    public class GetAllPendingReturnBooksQueryHandler : IRequestHandler<GetAllPendingReturnBooksQuery, PaginatedResult<BookIssueInfo>>
    {
        private readonly IMyAppDbContext _context;

        public GetAllPendingReturnBooksQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<BookIssueInfo>> Handle(GetAllPendingReturnBooksQuery request, CancellationToken cancellationToken)
        {
            var query = _context.BookIssues
                .Include(bi => bi.Book)
                .Include(bi => bi.LibraryMember)
                    .ThenInclude(m => m.Person)
                .Where(bi => !bi.IsDeleted)
                .Where(bi => bi.ReturnDate == null) // Only books not yet returned
                .Where(bi => bi.Status == "Issued" || bi.Status == "Renew") // Pending statuses
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

            // Filter by specific member
            if (request.LibraryMemberId.HasValue)
            {
                query = query.Where(bi => bi.LibraryMemberId == request.LibraryMemberId.Value);
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