// POS.Application/Features/User/CreateUserCommand.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        public List<int> RoleIds { get; set; } = new();
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

            RuleFor(x => x.RoleIds)
                .NotNull().WithMessage("Role IDs list is required");

            RuleForEach(x => x.RoleIds)
                .MustAsync(RoleExists).WithMessage("One or more roles not found");
        }

        private async Task<bool> BeUniqueUsername(string username, CancellationToken cancellationToken)
        {
            return !await _context.Persons.AnyAsync(p => p.Username == username, cancellationToken);
        }

        private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
        {
            return !await _context.Persons.AnyAsync(p => p.Email == email, cancellationToken);
        }

        private async Task<bool> RoleExists(int roleId, CancellationToken cancellationToken)
        {
            return await _context.Roles.AnyAsync(r => r.Id == roleId && !r.IsDeleted, cancellationToken);
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
            if (string.IsNullOrWhiteSpace(request.Username.Trim()))
            {
                return ApiResponse<UserInfo>.BadRequest("Username is required");
            }
            if (request.Username.Length < 3)
            {
                return ApiResponse<UserInfo>.BadRequest("Username must enter at least 3 character");
            }
            var isDuplicateUsername = await _context.Persons.AnyAsync(p => p.Username == request.Username);
            if (isDuplicateUsername)
            {
                return ApiResponse<UserInfo>.BadRequest("Username already exists");
            }
            var isDuplicateEmail = await _context.Persons.AnyAsync(p => p.Email == request.Email && p.IsDeleted == false);
            if (isDuplicateEmail)
            {
                return ApiResponse<UserInfo>.BadRequest("Email already exists");
            }
            var password = request.Password?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(password))
            {
                return ApiResponse<UserInfo>.BadRequest("Password is required");
            }

            if (password.Length < 8)
            {
                return ApiResponse<UserInfo>.BadRequest("Password must be at least 8 characters");
            }

            if (!Regex.IsMatch(password, "[A-Z]"))
            {
                return ApiResponse<UserInfo>.BadRequest("Password must contain at least one uppercase letter");
            }

            if (!Regex.IsMatch(password, "[a-z]"))
            {
                return ApiResponse<UserInfo>.BadRequest("Password must contain at least one lowercase letter");
            }

            if (!Regex.IsMatch(password, @"\d"))
            {
                return ApiResponse<UserInfo>.BadRequest("Password must contain at least one number");
            }

            if (!Regex.IsMatch(password, @"[!@#$%^&*]"))
            {
                return ApiResponse<UserInfo>.BadRequest("Password must contain at least one special character (!@#$%^&*)");
            }

            // Validate roles exist
            if (request.RoleIds.Any())
            {
                var validRoleIds = await _context.Roles
                    .Where(r => request.RoleIds.Contains(r.Id) && !r.IsDeleted)
                    .Select(r => r.Id)
                    .ToListAsync(cancellationToken);

                var invalidRoleIds = request.RoleIds.Except(validRoleIds).ToList();
                if (invalidRoleIds.Any())
                {
                    return ApiResponse<UserInfo>.BadRequest($"Invalid role IDs: {string.Join(", ", invalidRoleIds)}");
                }
            }

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

            // Assign roles
            if (request.RoleIds.Any())
            {
                var personRoles = request.RoleIds.Select(roleId => new PersonRole
                {
                    PersonId = person.Id,
                    RoleId = roleId
                }).ToList();

                _context.PersonRoles.AddRange(personRoles);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Load roles for response
            var roles = await _context.Roles
                .Where(r => request.RoleIds.Contains(r.Id) && !r.IsDeleted)
                .Select(r => new RoleBasicInfo
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description
                })
                .ToListAsync(cancellationToken);

            var userInfo = new UserInfo
            {
                Id = person.Id,
                Username = person.Username,
                ImageProfile = person.ImageProfile,
                Email = person.Email,
                FirstName = person.FirstName,
                LastName = person.LastName,
                PhoneNumber = person.PhoneNumber,
                IsActive = person.IsActive,
                Roles = roles
            };

            return ApiResponse<UserInfo>.Created(userInfo, "User created successfully");
        }
    }
}