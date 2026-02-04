// POS.Application/Features/Auth/RegisterCommand.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PersonEntity = POS.Domain.Entities.Person;

namespace POS.Application.Features.Auth
{
    public record RegisterCommand : IRequest<ApiResponse<AuthResponse>>
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }

    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        private readonly IMyAppDbContext _context;

        public RegisterCommandValidator(IMyAppDbContext context)
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
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one number");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Passwords do not match");

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

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, ApiResponse<AuthResponse>>
    {
        private readonly IMyAppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;

        public RegisterCommandHandler(
            IMyAppDbContext context,
            IPasswordHasher passwordHasher,
            IJwtService jwtService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
        }

        public async Task<ApiResponse<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            // 1. Password Hashing
            var hashedPassword = _passwordHasher.HashPassword(request.Password);

            // 2. Initial Validations (Manual checks as per your original code)
            if (string.IsNullOrWhiteSpace(request.Username?.Trim()))
                return ApiResponse<AuthResponse>.BadRequest("Username is required");

            var isDuplicateUsername = await _context.Persons.AnyAsync(p => p.Username == request.Username, cancellationToken);
            if (isDuplicateUsername)
                return ApiResponse<AuthResponse>.BadRequest("Username already exists");

            var isDuplicateEmail = await _context.Persons.AnyAsync(p => p.Email == request.Email && !p.IsDeleted, cancellationToken);
            if (isDuplicateEmail)
                return ApiResponse<AuthResponse>.BadRequest("Email already exists");

            // 3. Create the Person Entity
            var person = new PersonEntity
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = hashedPassword,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            // 4. AUTO-ASSIGN "User" ROLE
            // This looks for the role named "User" in your Database
            var defaultRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "User" && !r.IsDeleted, cancellationToken);

            if (defaultRole != null)
            {
                person.PersonRoles.Add(new PersonRole
                {
                    Person = person,
                    RoleId = defaultRole.Id
                });
            }

            // 5. Save to Database
            _context.Persons.Add(person);
            await _context.SaveChangesAsync(cancellationToken);

            // 6. Generate Tokens
            // We pass "User" in the roles list so the JWT contains the correct permissions
            var userRoles = new List<string> { "User" };
            var accessToken = _jwtService.GenerateAccessToken(person.Id, person.Username, userRoles);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // 7. Save Refresh Token
            var userRefreshToken = new RefreshToken
            {
                Token = refreshToken,
                PersonId = person.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                CreatedDate = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(userRefreshToken);
            await _context.SaveChangesAsync(cancellationToken);

            // 8. Final Response
            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = person.Id,
                Username = person.Username,
                Email = person.Email,
                FirstName = person.FirstName,
                LastName = person.LastName
            };

            return ApiResponse<AuthResponse>.Created(response, "User registered successfully");
        }
    }
}

