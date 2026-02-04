// POS.Application/Features/Dashboard/GetTodayDueBooksQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Application.Features.BookIssue;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Dashboard
{
    /// <summary>
    /// Get books that are due today
    /// </summary>
    public class GetTodayDueBooksQuery : PaginationRequest, IRequest<PaginatedResult<BookIssueInfo>>
    {
        public string? Search { get; set; }
    }

    public class GetTodayDueBooksQueryHandler : IRequestHandler<GetTodayDueBooksQuery, PaginatedResult<BookIssueInfo>>
    {
        private readonly IMyAppDbContext _context;

        public GetTodayDueBooksQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<BookIssueInfo>> Handle(GetTodayDueBooksQuery request, CancellationToken cancellationToken)
        {
            var today = DateTimeOffset.UtcNow.Date;

            var query = _context.BookIssues
                .Include(bi => bi.Book)
                .Include(bi => bi.LibraryMember)
                    .ThenInclude(m => m.Person)
                .Where(bi => !bi.IsDeleted)
                .Where(bi => bi.ReturnDate == null)
                .Where(bi => bi.DueDate.Date == today)
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

            // Order by issue date
            query = query.OrderBy(bi => bi.IssueDate);

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