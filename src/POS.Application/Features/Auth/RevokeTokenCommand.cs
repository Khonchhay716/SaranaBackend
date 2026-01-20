// POS.Application/Features/Auth/RevokeTokenCommand.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Auth
{
    public record RevokeTokenCommand : IRequest<ApiResponse>
    {
        public int UserId { get; set; }
    }

    public class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
    {
        public RevokeTokenCommandValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("Valid user ID is required");
        }
    }

    public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;

        public RevokeTokenCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.PersonId == request.UserId && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"All tokens for user {request.UserId} have been revoked");
        }
    }
}