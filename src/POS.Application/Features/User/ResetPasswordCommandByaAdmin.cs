// POS.Application/Features/User/ResetPasswordCommand.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.User
{
    public record ResetPasswordCommand : IRequest<ApiResponse>
    {
        public int    UserId          { get; set; }
        public string NewPassword     { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Validator
    // ─────────────────────────────────────────────────────────────────────────
    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            // ── UserId ────────────────────────────────────────────────────────
            RuleFor(x => x.UserId)
                .GreaterThan(0)
                    .WithMessage("A valid User ID is required.");

            // ── New Password ──────────────────────────────────────────────────
            RuleFor(x => x.NewPassword)
                .NotEmpty()
                    .WithMessage("New password is required.")
                .MinimumLength(8)
                    .WithMessage("New password must be at least 8 characters.")
                .Matches("[A-Z]")
                    .WithMessage("New password must contain at least one uppercase letter (A-Z).")
                .Matches("[a-z]")
                    .WithMessage("New password must contain at least one lowercase letter (a-z).")
                .Matches(@"\d")
                    .WithMessage("New password must contain at least one number (0-9).")
                .Matches(@"[!@#$%^&*]")
                    .WithMessage("New password must contain at least one special character (!@#$%^&*).");

            // ── Confirm Password ──────────────────────────────────────────────
            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                    .WithMessage("Confirm password is required.")
                // ✅ Must() for cross-property comparison
                .Must((cmd, confirmPwd) => confirmPwd == cmd.NewPassword)
                    .WithMessage("Confirm password does not match the new password.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Handler
    // ─────────────────────────────────────────────────────────────────────────
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ApiResponse>
    {
        private readonly IMyAppDbContext     _context;
        private readonly IPasswordHasher     _passwordHasher;
        private readonly ICurrentUserService _currentUserService;

        public ResetPasswordCommandHandler(
            IMyAppDbContext     context,
            IPasswordHasher     passwordHasher,
            ICurrentUserService currentUserService)
        {
            _context            = context;
            _passwordHasher     = passwordHasher;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            // ── 1. Only admin with user:update permission can do this ─────────
            var permissions = await _currentUserService.GetPermissionsAsync();
            if (!permissions.Contains("user:update"))
                return ApiResponse.Forbidden("You do not have permission to reset another user's password.");

            // ── 2. Load target user ───────────────────────────────────────────
            var person = await _context.Persons
                .FirstOrDefaultAsync(p => p.Id == request.UserId && !p.IsDeleted, cancellationToken);

            if (person == null)
                return ApiResponse.NotFound("User not found.");

            // ── 3. New password: empty check ──────────────────────────────────
            var newPassword = request.NewPassword?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(newPassword))
                return ApiResponse.BadRequest("New password is required.");

            // ── 4. New password: complexity rules ─────────────────────────────
            if (newPassword.Length < 8)
                return ApiResponse.BadRequest("New password must be at least 8 characters.");
            if (!Regex.IsMatch(newPassword, "[A-Z]"))
                return ApiResponse.BadRequest("New password must contain at least one uppercase letter (A-Z).");
            if (!Regex.IsMatch(newPassword, "[a-z]"))
                return ApiResponse.BadRequest("New password must contain at least one lowercase letter (a-z).");
            if (!Regex.IsMatch(newPassword, @"\d"))
                return ApiResponse.BadRequest("New password must contain at least one number (0-9).");
            if (!Regex.IsMatch(newPassword, @"[!@#$%^&*]"))
                return ApiResponse.BadRequest("New password must contain at least one special character (!@#$%^&*).");

            // ── 5. Confirm password must match ────────────────────────────────
            var confirmPassword = request.ConfirmPassword?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(confirmPassword))
                return ApiResponse.BadRequest("Confirm password is required.");
            if (confirmPassword != newPassword)
                return ApiResponse.BadRequest("Confirm password does not match the new password.");

            // ── 6. Hash + save — no current password needed ───────────────────
            person.PasswordHash = _passwordHasher.HashPassword(newPassword);
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Password for user ID {request.UserId} has been reset successfully.");
        }
    }
}