// POS.Application/Features/LibraryMember/ApproveLibraryMemberCommand.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.LibraryMember
{
    public record ApproveLibraryMemberCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
    }

    public class ApproveLibraryMemberCommandHandler : IRequestHandler<ApproveLibraryMemberCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ApproveLibraryMemberCommandHandler(IMyAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse> Handle(ApproveLibraryMemberCommand request, CancellationToken cancellationToken)
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

            // Check if already approved
            if (member.Status == (int)LibraryMemberStatus.Approved)
            {
                return ApiResponse.BadRequest("Library member is already approved");
            }

            // Check if cancelled or rejected
            if (member.Status == (int)LibraryMemberStatus.Cancelled)
            {
                return ApiResponse.BadRequest("Cannot approve a cancelled membership");
            }

            // Update status to approved
            member.ApproveBy = currentUserId.Value; // Set to current user ID
            member.Status = (int)LibraryMemberStatus.Approved;
            member.IsActive = true; // Activate when approved
            member.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Library member with id {request.Id} has been approved successfully");
        }
    }
}