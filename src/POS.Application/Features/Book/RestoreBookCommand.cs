// POS.Application/Features/Book/RestoreBookCommand.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Book
{
    public record RestoreBookCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
    }

    public class RestoreBookCommandHandler : IRequestHandler<RestoreBookCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;

        public RestoreBookCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse> Handle(RestoreBookCommand request, CancellationToken cancellationToken)
        {
            var book = await _context.Books
                .FirstOrDefaultAsync(x => x.Id == request.Id && x.IsDeleted, cancellationToken);

            if (book == null)
            {
                return ApiResponse.NotFound($"Deleted book with id {request.Id} was not found");
            }

            // Restore the book
            book.IsDeleted = false;
            book.DeletedDate = null;
            book.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Book with id {request.Id} restored successfully");
        }
    }
}