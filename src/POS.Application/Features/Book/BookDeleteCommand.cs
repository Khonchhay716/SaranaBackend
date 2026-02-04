// POS.Application/Features/Book/DeleteBookCommand.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Book
{
    public record DeleteBookCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
    }

    public class DeleteBookCommandHandler : IRequestHandler<DeleteBookCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;

        public DeleteBookCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse> Handle(DeleteBookCommand request, CancellationToken cancellationToken)
        {
            var book = await _context.Books
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (book == null)
            {
                return ApiResponse.NotFound($"Book with id {request.Id} was not found");
            }

            // Soft delete
            book.IsDeleted = true;
            book.DeletedDate = DateTimeOffset.UtcNow;
            book.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Book with id {request.Id} deleted successfully");
        }
    }
}