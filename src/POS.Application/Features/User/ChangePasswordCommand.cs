// // POS.Application/Features/User/ChangePasswordCommand.cs
// using FluentValidation;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Dto;
// using POS.Application.Common.Interfaces;
// using System.Threading;
// using System.Threading.Tasks;

// namespace POS.Application.Features.User
// {
//     public record ChangePasswordCommand : IRequest<ApiResponse>
//     {
//         public int UserId { get; set; }
//         public string CurrentPassword { get; set; } = string.Empty;
//         public string NewPassword { get; set; } = string.Empty;
//         public string ConfirmPassword { get; set; } = string.Empty;
//     }

//     public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
//     {
//         public ChangePasswordCommandValidator()
//         {
//             RuleFor(x => x.UserId)
//                 .GreaterThan(0).WithMessage("Valid user ID is required");

//             RuleFor(x => x.CurrentPassword)
//                 .NotEmpty().WithMessage("Current password is required");

//             RuleFor(x => x.NewPassword)
//                 .NotEmpty().WithMessage("New password is required")
//                 .MinimumLength(8).WithMessage("Password must be at least 8 characters")
//                 .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
//                 .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
//                 .Matches(@"[0-9]").WithMessage("Password must contain at least one number")
//                 .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password");

//             RuleFor(x => x.ConfirmPassword)
//                 .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
//         }
//     }

//     public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, ApiResponse>
//     {
//         private readonly IMyAppDbContext _context;
//         private readonly IPasswordHasher _passwordHasher;
//         private readonly ICurrentUserService _currentUserService;

//         public ChangePasswordCommandHandler(
//             IMyAppDbContext context,
//             IPasswordHasher passwordHasher,
//             ICurrentUserService currentUserService)
//         {
//             _context = context;
//             _passwordHasher = passwordHasher;
//             _currentUserService = currentUserService;
//         }

//         public async Task<ApiResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
//         {
//             // Users can only change their own password unless they have admin permissions
//             if (_currentUserService.UserId != request.UserId)
//             {
//                 var permissions = await _currentUserService.GetPermissionsAsync();
//                 if (!permissions.Contains("person:update"))
//                 {
//                     return ApiResponse.Forbidden("You can only change your own password");
//                 }
//             }

//             var person = await _context.Persons
//                 .FirstOrDefaultAsync(p => p.Id == request.UserId && !p.IsDeleted, cancellationToken);

//             if (person == null)
//             {
//                 return ApiResponse.NotFound("User not found");
//             }

//             if (!_passwordHasher.VerifyPassword(request.CurrentPassword, person.PasswordHash))
//             {
//                 return ApiResponse.BadRequest("Current password is incorrect");
//             }

//             person.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
//             await _context.SaveChangesAsync(cancellationToken);

//             return ApiResponse.Ok("Password changed successfully");
//         }
//     }
// }



// POS.Application/Features/User/ChangePasswordCommand.cs
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
    public record ChangePasswordCommand : IRequest<ApiResponse>
    {
        public int UserId { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0)
                    .WithMessage("A valid User ID is required.");
            RuleFor(x => x.CurrentPassword)
                .NotEmpty()
                    .WithMessage("Current password is required.");

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
                    .WithMessage("New password must contain at least one special character (!@#$%^&*).")
                .NotEqual(x => x.CurrentPassword)
                    .WithMessage("New password must be different from the current password.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                    .WithMessage("Confirm password is required.")
                .Equal(x => x.NewPassword)
                    .WithMessage("Confirm password does not match the new password.");
        }
    }
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICurrentUserService _currentUserService;

        public ChangePasswordCommandHandler(
            IMyAppDbContext context,
            IPasswordHasher passwordHasher,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            if (_currentUserService.UserId != request.UserId)
            {
                var permissions = await _currentUserService.GetPermissionsAsync();
                if (!permissions.Contains("person:update"))
                    return ApiResponse.Forbidden("You are not allowed to change another user's password.");
            }

            var person = await _context.Persons
                .FirstOrDefaultAsync(p => p.Id == request.UserId && !p.IsDeleted, cancellationToken);

            if (person == null)
                return ApiResponse.NotFound("User not found.");

            var currentPassword = request.CurrentPassword?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(currentPassword))
                return ApiResponse.BadRequest("Current password is required.");

            if (!_passwordHasher.VerifyPassword(currentPassword, person.PasswordHash))
                return ApiResponse.BadRequest("Current password is incorrect.");

            var newPassword = request.NewPassword?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(newPassword))
                return ApiResponse.BadRequest("New password is required.");

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

            // ── 7. New password must differ from current ──────────────────────
            if (newPassword == currentPassword)
                return ApiResponse.BadRequest("New password must be different from the current password.");

            // ── 8. Confirm password must match ────────────────────────────────
            var confirmPassword = request.ConfirmPassword?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(confirmPassword))
                return ApiResponse.BadRequest("Confirm password is required.");
            if (confirmPassword != newPassword)
                return ApiResponse.BadRequest("Confirm password does not match the new password.");

            // ── 9. Hash + save ────────────────────────────────────────────────
            person.PasswordHash = _passwordHasher.HashPassword(newPassword);
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok("Password changed successfully.");
        }
    }
}