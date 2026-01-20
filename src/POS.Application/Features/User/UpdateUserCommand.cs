// POS.Application/Features/User/UpdateUserCommand.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.User
{
    public record UpdateUserCommand : IRequest<ApiResponse<UserInfo>>
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string ImageProfile { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        private readonly IMyAppDbContext _context;

        public UpdateUserCommandValidator(IMyAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("Valid user ID is required");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MustAsync(BeUniqueEmail).WithMessage("Email already exists");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");
        }

        private async Task<bool> BeUniqueEmail(UpdateUserCommand command, string email, CancellationToken cancellationToken)
        {
            return !await _context.Persons.AnyAsync(
                p => p.Email == email && p.Id != command.UserId, 
                cancellationToken);
        }
    }

    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, ApiResponse<UserInfo>>
    {
        private readonly IMyAppDbContext _context;

        public UpdateUserCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<UserInfo>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var person = await _context.Persons
                .FirstOrDefaultAsync(p => p.Id == request.UserId && !p.IsDeleted, cancellationToken);

            if (person == null)
            {
                return ApiResponse<UserInfo>.NotFound("User not found");
            }

            person.Email = request.Email;
            person.ImageProfile = request.ImageProfile;
            person.FirstName = request.FirstName;
            person.LastName = request.LastName;
            person.PhoneNumber = request.PhoneNumber;
            person.IsActive = request.IsActive;

            await _context.SaveChangesAsync(cancellationToken);

            var userInfo = new UserInfo
            {
                Id = person.Id,
                Username = person.Username,
                ImageProfile = person.ImageProfile,
                Email = person.Email,
                FirstName = person.FirstName,
                LastName = person.LastName,
                PhoneNumber = person.PhoneNumber,
                IsActive = person.IsActive
            };

            return ApiResponse<UserInfo>.Ok(userInfo, "User updated successfully");
        }
    }
}