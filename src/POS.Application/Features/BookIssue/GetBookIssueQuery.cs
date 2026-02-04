// POS.Application/Features/BookIssue/GetBookIssueQuery.cs
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.BookIssue
{
    public record GetBookIssueQuery : IRequest<ApiResponse<BookIssueInfo>>
    {
        public int Id { get; set; }
    }

    public class GetBookIssueQueryHandler : IRequestHandler<GetBookIssueQuery, ApiResponse<BookIssueInfo>>
    {
        private readonly IMyAppDbContext _context;

        public GetBookIssueQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<BookIssueInfo>> Handle(GetBookIssueQuery request, CancellationToken cancellationToken)
        {
            var bookIssue = await _context.BookIssues
                .Include(bi => bi.Book)
                .Include(bi => bi.LibraryMember)
                    .ThenInclude(m => m.Person)
                .FirstOrDefaultAsync(bi => bi.Id == request.Id && !bi.IsDeleted, cancellationToken);

            if (bookIssue == null)
            {
                return ApiResponse<BookIssueInfo>.NotFound($"Book issue with id {request.Id} not found");
            }

            // Use Adapt for mapping
            var result = bookIssue.Adapt<BookIssueInfo>();
            result.BookTitle = bookIssue.Book.Title;
            result.BookAuthor = bookIssue.Book.Author;
            result.MemberName = $"{bookIssue.LibraryMember.Person.FirstName} {bookIssue.LibraryMember.Person.LastName}";
            result.MembershipNo = bookIssue.LibraryMember.MembershipNo;

            return ApiResponse<BookIssueInfo>.Ok(result);
        }
    }
}