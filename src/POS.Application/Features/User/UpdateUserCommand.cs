// POS.Application/Features/User/UpdateUserCommand.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.User
{
    public record UpdateUserCommand : IRequest<ApiResponse<UserInfo>>
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string ImageProfile { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public List<int> RoleIds { get; set; } = new();
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

            RuleFor(x => x.RoleIds)
                .NotNull().WithMessage("Role IDs list is required");

            RuleForEach(x => x.RoleIds)
                .MustAsync(RoleExists).WithMessage("One or more roles not found");
        }

        private async Task<bool> BeUniqueEmail(UpdateUserCommand command, string email, CancellationToken cancellationToken)
        {
            return !await _context.Persons.AnyAsync(
                p => p.Email == email && p.Id != command.UserId, 
                cancellationToken);
        }

        private async Task<bool> RoleExists(int roleId, CancellationToken cancellationToken)
        {
            return await _context.Roles.AnyAsync(r => r.Id == roleId && !r.IsDeleted, cancellationToken);
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
                .Include(p => p.PersonRoles)
                .FirstOrDefaultAsync(p => p.Id == request.UserId && !p.IsDeleted, cancellationToken);

            if (person == null)
            {
                return ApiResponse<UserInfo>.NotFound("User not found");
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
            person.Username = request.Username;
            person.Email = request.Email;
            person.ImageProfile = request.ImageProfile;
            person.FirstName = request.FirstName;
            person.LastName = request.LastName;
            person.PhoneNumber = request.PhoneNumber;
            person.IsActive = request.IsActive;

            // Update roles
            _context.PersonRoles.RemoveRange(person.PersonRoles);

            if (request.RoleIds.Any())
            {
                var personRoles = request.RoleIds.Select(roleId => new PersonRole
                {
                    PersonId = person.Id,
                    RoleId = roleId
                }).ToList();

                _context.PersonRoles.AddRange(personRoles);
            }

            await _context.SaveChangesAsync(cancellationToken);

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

            return ApiResponse<UserInfo>.Ok(userInfo, "User updated successfully");
        }
    }
}