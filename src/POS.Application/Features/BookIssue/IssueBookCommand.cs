// POS.Application/Features/BookIssue/IssueBookCommand.cs
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.BookIssue
{
    public class IssueBookCommand : IRequest<ApiResponse<BookIssueInfo>>
    {
        public int BookId { get; set; }
        public int LibraryMemberId { get; set; }
        public int DueDays { get; set; } = 14; 
        public string? Notes { get; set; }
    }

    public class IssueBookCommandValidator : AbstractValidator<IssueBookCommand>
    {
        public IssueBookCommandValidator()
        {
            RuleFor(x => x.BookId).GreaterThan(0).WithMessage("Book ID is required");
            RuleFor(x => x.LibraryMemberId).GreaterThan(0).WithMessage("Library Member ID is required");
            RuleFor(x => x.DueDays).GreaterThan(0).WithMessage("Due days must be greater than 0");
        }
    }

    public class IssueBookCommandHandler : IRequestHandler<IssueBookCommand, ApiResponse<BookIssueInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public IssueBookCommandHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<BookIssueInfo>> Handle(IssueBookCommand request, CancellationToken cancellationToken)
        {
            // 1. Verify Book availability
            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == request.BookId && !b.IsDeleted, cancellationToken);

            if (book == null)
            {
                return ApiResponse<BookIssueInfo>.NotFound("Book not found");
            }

            if (book.AvailableQty <= 0)
            {
                return ApiResponse<BookIssueInfo>.BadRequest("Book is currently out of stock");
            }

            // 2. Verify Library Member
            var member = await _context.LibraryMembers
                .Include(m => m.Person)
                .FirstOrDefaultAsync(m => m.Id == request.LibraryMemberId && !m.IsDeleted, cancellationToken);

            if (member == null)
            {
                return ApiResponse<BookIssueInfo>.NotFound("Library member not found");
            }

            if (!member.IsActive)
            {
                return ApiResponse<BookIssueInfo>.BadRequest("Library member account is not active");
            }

            // 3. Check borrow limits
            var currentIssuesCount = await _context.BookIssues
                .CountAsync(bi => bi.LibraryMemberId == request.LibraryMemberId 
                    && bi.Status == "Issued" 
                    && !bi.IsDeleted, cancellationToken);

            if (currentIssuesCount >= member.MaxBooksAllowed)
            {
                return ApiResponse<BookIssueInfo>.BadRequest(
                    $"Member has reached the maximum allowed limit ({member.MaxBooksAllowed} books)");
            }

            // 4. Check for overdue books
            var hasOverdue = await _context.BookIssues
                .AnyAsync(bi => bi.LibraryMemberId == request.LibraryMemberId 
                    && bi.Status == "Overdue" 
                    && !bi.IsDeleted, cancellationToken);

            if (hasOverdue)
            {
                return ApiResponse<BookIssueInfo>.BadRequest(
                    "Member has overdue books. Please return them before issuing new ones.");
            }

            // 5. Get Staff ID from Current User Session
            var currentUserId = _currentUserService.UserId;
            if (currentUserId == null)
            {
                return ApiResponse<BookIssueInfo>.Unauthorized("Authentication required. Please log in as staff.");
            }

            // 6. Create the Book Issue record
            var bookIssue = request.Adapt<Domain.Entities.BookIssue>();
            bookIssue.IssuedByPersonId = currentUserId.Value; // Automatic Staff ID
            bookIssue.IssueDate = DateTimeOffset.UtcNow;
            bookIssue.DueDate = DateTimeOffset.UtcNow.AddDays(request.DueDays);
            bookIssue.Status = "Issued";
            bookIssue.CreatedDate = DateTimeOffset.UtcNow;
            bookIssue.IsDeleted = false;

            _context.BookIssues.Add(bookIssue);

            // 7. Update Inventory
            book.AvailableQty -= 1;
            book.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // 8. Prepare Response
            var result = bookIssue.Adapt<BookIssueInfo>();
            result.BookTitle = book.Title;
            result.BookAuthor = book.Author;
            result.MemberName = $"{member.Person.FirstName} {member.Person.LastName}";
            result.MembershipNo = member.MembershipNo;

            return ApiResponse<BookIssueInfo>.Created(result, "Book issued successfully");
        }
    }
}