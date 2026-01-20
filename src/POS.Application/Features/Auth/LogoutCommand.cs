// POS.Application/Features/Auth/LogoutCommand.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Auth
{
    public record LogoutCommand : IRequest<ApiResponse>
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
    {
        public LogoutCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token is required");
        }
    }

    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;

        public LogoutCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

            if (token != null)
            {
                token.IsRevoked = true;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return ApiResponse.Ok("Logged out successfully");
        }
    }
}