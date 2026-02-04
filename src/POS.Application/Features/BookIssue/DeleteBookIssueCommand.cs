// POS.Application/Features/BookIssue/DeleteBookIssueCommand.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.BookIssue
{
    public record DeleteBookIssueCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
    }

    public class DeleteBookIssueCommandHandler : IRequestHandler<DeleteBookIssueCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;

        public DeleteBookIssueCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse> Handle(DeleteBookIssueCommand request, CancellationToken cancellationToken)
        {
            var bookIssue = await _context.BookIssues
                .FirstOrDefaultAsync(bi => bi.Id == request.Id && !bi.IsDeleted, cancellationToken);

            if (bookIssue == null)
            {
                return ApiResponse.NotFound($"Book issue with id {request.Id} not found");
            }

            // Soft delete
            bookIssue.IsDeleted = true;
            bookIssue.DeletedDate = DateTimeOffset.UtcNow;
            bookIssue.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Book issue with id {request.Id} deleted successfully");
        }
    }
}