// POS.Application/Features/LibraryMember/RejectLibraryMemberCommand.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.LibraryMember
{
    public record RejectLibraryMemberCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class RejectLibraryMemberCommandHandler : IRequestHandler<RejectLibraryMemberCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public RejectLibraryMemberCommandHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse> Handle(RejectLibraryMemberCommand request, CancellationToken cancellationToken)
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

            // Check if already rejected
            if (member.Status == (int)LibraryMemberStatus.Rejected)
            {
                return ApiResponse.BadRequest("Library member is already rejected");
            }

            // Check if cancelled
            if (member.Status == (int)LibraryMemberStatus.Cancelled)
            {
                return ApiResponse.BadRequest("Cannot reject a cancelled membership");
            }

            // Update status to rejected
            member.ApproveBy = currentUserId.Value; // Set to current user ID
            member.Status = (int)LibraryMemberStatus.Rejected;
            member.IsActive = false; // Deactivate when rejected
            member.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Library member with id {request.Id} has been rejected successfully");
        }
    }
}