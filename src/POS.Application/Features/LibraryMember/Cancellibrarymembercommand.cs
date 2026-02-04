// POS.Application/Features/LibraryMember/CancelLibraryMemberCommand.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.LibraryMember
{
    public record CancelLibraryMemberCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
        public string? CancellationReason { get; set; }
    }

    public class CancelLibraryMemberCommandHandler : IRequestHandler<CancelLibraryMemberCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CancelLibraryMemberCommandHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse> Handle(CancelLibraryMemberCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;

            if (currentUserId == null)
            {
                return ApiResponse.Unauthorized("User not authenticated");
            }

            var member = await _context.LibraryMembers
                .FirstOrDefaultAsync(m => m.Id == request.Id && !m.IsDeleted, cancellationToken);

            if (member == null)
            {
                return ApiResponse.NotFound($"Library member with id {request.Id} not found");
            }

            // Check if already cancelled
            if (member.Status == (int)LibraryMemberStatus.Cancelled)
            {
                return ApiResponse.BadRequest("Library member is already cancelled");
            }

            // Update status to cancelled
            member.ApproveBy = currentUserId.Value; // Set to current user ID (who processed the cancellation)
            member.Status = (int)LibraryMemberStatus.Cancelled;
            member.IsActive = false; // Deactivate when cancelled
            member.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Library member with id {request.Id} has been cancelled successfully");
        }
    }
}