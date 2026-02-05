// // POS.Application/Features/BookIssue/RenewBookCommand.cs
// using Mapster;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Dto;
// using POS.Application.Common.Interfaces;
// using System;
// using System.Threading;
// using System.Threading.Tasks;

// namespace POS.Application.Features.BookIssue
// {
//     public class RenewBookCommand : IRequest<ApiResponse<BookIssueInfo>>
//     {
//         public int Id { get; set; }
//         public DateTimeOffset NewDueDate { get; set; }
//         public string? Notes { get; set; }
//     }

//     public class RenewBookCommandHandler
//         : IRequestHandler<RenewBookCommand, ApiResponse<BookIssueInfo>>
//     {
//         private readonly IMyAppDbContext _context;

//         public RenewBookCommandHandler(IMyAppDbContext context)
//         {
//             _context = context;
//         }

//         public async Task<ApiResponse<BookIssueInfo>> Handle(
//             RenewBookCommand request,
//             CancellationToken cancellationToken)
//         {
//             var bookIssue = await _context.BookIssues
//                 .Include(bi => bi.Book)
//                 .Include(bi => bi.LibraryMember)
//                     .ThenInclude(m => m.Person)
//                 .FirstOrDefaultAsync(
//                     bi => bi.Id == request.Id && !bi.IsDeleted,
//                     cancellationToken);

//             if (bookIssue == null)
//                 return ApiResponse<BookIssueInfo>.NotFound("Book issue record not found");

//             // Simple validations: new date must be > old due date AND > current date
//             if (request.NewDueDate <= bookIssue.DueDate)
//                 return ApiResponse<BookIssueInfo>.BadRequest("New due date must be later than current due date");

//             if (request.NewDueDate <= DateTimeOffset.UtcNow)
//                 return ApiResponse<BookIssueInfo>.BadRequest("New due date must be in the future");

//             // Update book issue
//             bookIssue.DueDate = request.NewDueDate;
//             bookIssue.Status = "Renewed";
//             bookIssue.UpdatedDate = DateTimeOffset.UtcNow;

//             if (!string.IsNullOrWhiteSpace(request.Notes))
//                 bookIssue.Notes = request.Notes;

//             await _context.SaveChangesAsync(cancellationToken);

//             // Build response
//             var result = bookIssue.Adapt<BookIssueInfo>();
//             result.BookTitle = bookIssue.Book.Title;
//             result.BookAuthor = bookIssue.Book.Author ?? string.Empty;
//             result.MemberName =
//                 $"{bookIssue.LibraryMember.Person.FirstName} {bookIssue.LibraryMember.Person.LastName}";
//             result.MembershipNo = bookIssue.LibraryMember.MembershipNo;

//             return ApiResponse<BookIssueInfo>.Ok(result, "Book renewed successfully");
//         }
//     }
// }



// POS.Application/Features/BookIssue/RenewBookCommand.cs
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
    public class RenewBookCommand : IRequest<ApiResponse<BookIssueInfo>>
    {
        public int Id { get; set; }
        public DateTimeOffset NewDueDate { get; set; }
        public string? Notes { get; set; }
    }

    public class RenewBookCommandHandler
        : IRequestHandler<RenewBookCommand, ApiResponse<BookIssueInfo>>
    {
        private readonly IMyAppDbContext _context;
        private const int MAX_RENEW_COUNT = 1;
        private const int MAX_EXTENSION_DAYS = 3;

        public RenewBookCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<BookIssueInfo>> Handle(
            RenewBookCommand request,
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

            // Validation 1: Check if book has already been returned
            if (bookIssue.ReturnDate.HasValue)
                return ApiResponse<BookIssueInfo>.BadRequest("Cannot renew a book that has already been returned");

            // Validation 2: Check maximum renew count (max 1 renewal allowed)
            if (bookIssue.RenewCount >= MAX_RENEW_COUNT)
                return ApiResponse<BookIssueInfo>.BadRequest(
                    $"Maximum renewal limit reached. This book can only be renewed {MAX_RENEW_COUNT} time(s)");

            // Validation 3: New due date must be after current due date
            if (request.NewDueDate <= bookIssue.DueDate)
                return ApiResponse<BookIssueInfo>.BadRequest("New due date must be later than current due date");

            // Validation 4: New due date must be in the future
            if (request.NewDueDate <= DateTimeOffset.UtcNow)
                return ApiResponse<BookIssueInfo>.BadRequest("New due date must be in the future");

            // Validation 5: Maximum extension period is 3 days after current due date
            var maxAllowedDueDate = bookIssue.DueDate.AddDays(MAX_EXTENSION_DAYS);
            if (request.NewDueDate > maxAllowedDueDate)
                return ApiResponse<BookIssueInfo>.BadRequest(
                    $"New due date cannot be more than {MAX_EXTENSION_DAYS} days after the current due date. " +
                    $"Maximum allowed date: {maxAllowedDueDate:yyyy-MM-dd}");

            // Update book issue
            bookIssue.DueDate = request.NewDueDate;
            bookIssue.Status = "Renewed";
            bookIssue.RenewCount += 1;
            bookIssue.LastRenewedDate = DateTimeOffset.UtcNow;
            bookIssue.UpdatedDate = DateTimeOffset.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Notes))
                bookIssue.Notes = request.Notes;

            await _context.SaveChangesAsync(cancellationToken);

            // Build response
            var result = bookIssue.Adapt<BookIssueInfo>();
            result.BookTitle = bookIssue.Book.Title;
            result.BookAuthor = bookIssue.Book.Author ?? string.Empty;
            result.MemberName =
                $"{bookIssue.LibraryMember.Person.FirstName} {bookIssue.LibraryMember.Person.LastName}";
            result.MembershipNo = bookIssue.LibraryMember.MembershipNo;

            return ApiResponse<BookIssueInfo>.Ok(result, "Book renewed successfully");
        }
    }
}