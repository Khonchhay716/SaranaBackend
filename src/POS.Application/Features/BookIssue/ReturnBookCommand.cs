// POS.Application/Features/BookIssue/ReturnBookCommand.cs
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.BookIssue
{
    public class ReturnBookCommand : IRequest<ApiResponse<BookIssueInfo>>
    {
        public int Id { get; set; }   // BookIssueId
        public string? Notes { get; set; }
    }

    public class ReturnBookCommandHandler 
        : IRequestHandler<ReturnBookCommand, ApiResponse<BookIssueInfo>>
    {
        private readonly IMyAppDbContext _context;

        public ReturnBookCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<BookIssueInfo>> Handle(
            ReturnBookCommand request,
            CancellationToken cancellationToken)
        {
            var bookIssue = await _context.BookIssues
                .Include(bi => bi.Book)
                .Include(bi => bi.LibraryMember)
                    .ThenInclude(m => m.Person)
                .FirstOrDefaultAsync(
                    bi => bi.Id == request.Id && !bi.IsDeleted,
                    cancellationToken);

            if (bookIssue == null)
                return ApiResponse<BookIssueInfo>.NotFound("Book issue record not found");

            // ✅ Return logic
            bookIssue.Status = "Returned";
            bookIssue.ReturnDate = DateTimeOffset.UtcNow;
            bookIssue.UpdatedDate = DateTimeOffset.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Notes))
                bookIssue.Notes = request.Notes;

            // Increase available quantity
            bookIssue.Book.AvailableQty += 1;
            bookIssue.Book.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Response
            var result = bookIssue.Adapt<BookIssueInfo>();
            result.BookTitle = bookIssue.Book.Title;
            result.BookAuthor = bookIssue.Book.Author ?? string.Empty;
            result.MemberName =
                $"{bookIssue.LibraryMember.Person.FirstName} {bookIssue.LibraryMember.Person.LastName}";
            result.MembershipNo = bookIssue.LibraryMember.MembershipNo;

            return ApiResponse<BookIssueInfo>.Ok(result, "Book returned successfully");
        }
    }
}
