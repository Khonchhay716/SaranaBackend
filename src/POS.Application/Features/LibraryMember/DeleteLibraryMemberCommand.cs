// POS.Application/Features/LibraryMember/DeleteLibraryMemberCommand.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.LibraryMember
{
    public record DeleteLibraryMemberCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
    }

    public class DeleteLibraryMemberCommandHandler : IRequestHandler<DeleteLibraryMemberCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;

        public DeleteLibraryMemberCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse> Handle(DeleteLibraryMemberCommand request, CancellationToken cancellationToken)
        {
            var member = await _context.LibraryMembers
                .FirstOrDefaultAsync(m => m.Id == request.Id && !m.IsDeleted, cancellationToken);

            if (member == null)
            {
                return ApiResponse.NotFound($"Library member with id {request.Id} not found");
            }

            // Soft delete
            member.IsDeleted = true;
            member.DeletedDate = DateTimeOffset.UtcNow;
            member.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Library member with id {request.Id} deleted successfully");
        }
    }
}