// POS.Application/Features/User/CreateUserCommand.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using PersonEntity = POS.Domain.Entities.Person;

namespace POS.Application.Features.User
{
    public record CreateUserCommand : IRequest<ApiResponse<UserInfo>>
    {
        public string Username { get; set; } = string.Empty;
        public string ImageProfile { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        private readonly IMyAppDbContext _context;

        public CreateUserCommandValidator(IMyAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters")
                .MaximumLength(50).WithMessage("Username must not exceed 50 characters")
                .MustAsync(BeUniqueUsername).WithMessage("Username already exists");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MustAsync(BeUniqueEmail).WithMessage("Email already exists");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");
        }

        private async Task<bool> BeUniqueUsername(string username, CancellationToken cancellationToken)
        {
            return !await _context.Persons.AnyAsync(p => p.Username == username, cancellationToken);
        }

        private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
        {
            return !await _context.Persons.AnyAsync(p => p.Email == email, cancellationToken);
        }
    }

    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, ApiResponse<UserInfo>>
    {
        private readonly IMyAppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public CreateUserCommandHandler(IMyAppDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<ApiResponse<UserInfo>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var hashedPassword = _passwordHasher.HashPassword(request.Password);

            var person = new PersonEntity
            {
                Username = request.Username,
                ImageProfile = request.ImageProfile,
                Email = request.Email,
                PasswordHash = hashedPassword,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                IsActive = request.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            _context.Persons.Add(person);
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

            return ApiResponse<UserInfo>.Created(userInfo, "User created successfully");
        }
    }
}