// using FluentValidation;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Dto;
// using POS.Application.Common.Interfaces;
// using System.Threading;
// using System.Threading.Tasks;

// namespace POS.Application.Features.User
// {
//     // The Command request structure
//     public record UpdatePasswordCommand : IRequest<ApiResponse<string>>
//     {
//         public string Email { get; set; } = string.Empty;
//         public string NewPassword { get; set; } = string.Empty;
//     }

//     // Validation logic for the new password
//     public class UpdatePasswordCommandValidator : AbstractValidator<UpdatePasswordCommand>
//     {
//         public UpdatePasswordCommandValidator()
//         {
//             RuleFor(x => x.Email)
//                 .NotEmpty().WithMessage("Email is required")
//                 .EmailAddress().WithMessage("Invalid email format");

//             RuleFor(x => x.NewPassword)
//                 .NotEmpty().WithMessage("Password is required")
//                 .MinimumLength(8).WithMessage("Password must be at least 8 characters")
//                 .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
//                 .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
//                 .Matches(@"\d").WithMessage("Password must contain at least one number")
//                 .Matches(@"[!@#$%^&*]").WithMessage("Password must contain at least one special character (!@#$%^&*)");
//         }
//     }

//     public class UpdatePasswordCommandHandler : IRequestHandler<UpdatePasswordCommand, ApiResponse<string>>
//     {
//         private readonly IMyAppDbContext _context;
//         private readonly IPasswordHasher _passwordHasher;

//         public UpdatePasswordCommandHandler(IMyAppDbContext context, IPasswordHasher passwordHasher)
//         {
//             _context = context;
//             _passwordHasher = passwordHasher;
//         }

//         public async Task<ApiResponse<string>> Handle(UpdatePasswordCommand request, CancellationToken cancellationToken)
//         {
//             var person = await _context.Persons
//                 .FirstOrDefaultAsync(p => p.Email == request.Email && !p.IsDeleted, cancellationToken);

//             if (person == null)
//             {
//                 return ApiResponse<string>.NotFound("User with this email was not found.");
//             }
//             person.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
//             await _context.SaveChangesAsync(cancellationToken);

//             return ApiResponse<string>.Ok("Password updated successfully");
//         }
//     }
// }




// POS.Application/Features/User/UpdatePasswordCommand.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.User
{
    public record UpdatePasswordCommand : IRequest<ApiResponse<string>>
    {
        public string Email { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UpdatePasswordCommandValidator : AbstractValidator<UpdatePasswordCommand>
    {
        public UpdatePasswordCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches(@"\d").WithMessage("Password must contain at least one number")
                .Matches(@"[!@#$%^&*]").WithMessage("Password must contain at least one special character (!@#$%^&*)");
        }
    }

    public class UpdatePasswordCommandHandler : IRequestHandler<UpdatePasswordCommand, ApiResponse<string>>
    {
        private readonly IMyAppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public UpdatePasswordCommandHandler(IMyAppDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<ApiResponse<string>> Handle(UpdatePasswordCommand request, CancellationToken cancellationToken)
        {
            var person = await _context.Persons
                .FirstOrDefaultAsync(p => p.Email == request.Email && !p.IsDeleted, cancellationToken);

            if (person == null)
                return ApiResponse<string>.NotFound("User with this email was not found.");

            person.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<string>.Ok("Password updated successfully");
        }
    }
}