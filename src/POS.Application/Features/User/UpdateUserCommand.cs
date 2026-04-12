// // POS.Application/Features/User/UpdateUserCommand.cs
// using FluentValidation;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Dto;
// using POS.Application.Common.Interfaces;
// using POS.Domain.Entities;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;

// namespace POS.Application.Features.User
// {
//     public record UpdateUserCommand : IRequest<ApiResponse<UserInfo>>
//     {
//         public int UserId { get; set; }
//         public string Email { get; set; } = string.Empty;
//         public string Username { get; set; } = string.Empty;
//         public string ImageProfile { get; set; } = string.Empty;
//         public string FirstName { get; set; } = string.Empty;
//         public string LastName { get; set; } = string.Empty;
//         public string? PhoneNumber { get; set; }
//         public bool IsActive { get; set; }
//         public List<int> RoleIds { get; set; } = new();
//     }

//     public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
//     {
//         private readonly IMyAppDbContext _context;

//         public UpdateUserCommandValidator(IMyAppDbContext context)
//         {
//             _context = context;

//             RuleFor(x => x.UserId)
//                 .GreaterThan(0).WithMessage("Valid user ID is required");

//             RuleFor(x => x.Email)
//                 .NotEmpty().WithMessage("Email is required")
//                 .EmailAddress().WithMessage("Invalid email format")
//                 .MustAsync(BeUniqueEmail).WithMessage("Email already exists");

//             RuleFor(x => x.FirstName)
//                 .NotEmpty().WithMessage("First name is required")
//                 .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

//             RuleFor(x => x.LastName)
//                 .NotEmpty().WithMessage("Last name is required")
//                 .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

//             RuleFor(x => x.RoleIds)
//                 .NotNull().WithMessage("Role IDs list is required");

//             RuleForEach(x => x.RoleIds)
//                 .MustAsync(RoleExists).WithMessage("One or more roles not found");
//         }

//         private async Task<bool> BeUniqueEmail(UpdateUserCommand command, string email, CancellationToken cancellationToken)
//         {
//             return !await _context.Persons.AnyAsync(
//                 p => p.Email == email && p.Id != command.UserId, 
//                 cancellationToken);
//         }

//         private async Task<bool> RoleExists(int roleId, CancellationToken cancellationToken)
//         {
//             return await _context.Roles.AnyAsync(r => r.Id == roleId && !r.IsDeleted, cancellationToken);
//         }
//     }

//     public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, ApiResponse<UserInfo>>
//     {
//         private readonly IMyAppDbContext _context;

//         public UpdateUserCommandHandler(IMyAppDbContext context)
//         {
//             _context = context;
//         }

//         public async Task<ApiResponse<UserInfo>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
//         {
//             var person = await _context.Persons
//                 .Include(p => p.PersonRoles)
//                 .FirstOrDefaultAsync(p => p.Id == request.UserId && !p.IsDeleted, cancellationToken);

//             if (person == null)
//             {
//                 return ApiResponse<UserInfo>.NotFound("User not found");
//             }

//             // Validate roles exist
//             if (request.RoleIds.Any())
//             {
//                 var validRoleIds = await _context.Roles
//                     .Where(r => request.RoleIds.Contains(r.Id) && !r.IsDeleted)
//                     .Select(r => r.Id)
//                     .ToListAsync(cancellationToken);

//                 var invalidRoleIds = request.RoleIds.Except(validRoleIds).ToList();
//                 if (invalidRoleIds.Any())
//                 {
//                     return ApiResponse<UserInfo>.BadRequest($"Invalid role IDs: {string.Join(", ", invalidRoleIds)}");
//                 }
//             }
//             person.Username = request.Username;
//             person.Email = request.Email;
//             person.ImageProfile = request.ImageProfile;
//             person.FirstName = request.FirstName;
//             person.LastName = request.LastName;
//             person.PhoneNumber = request.PhoneNumber;
//             person.IsActive = request.IsActive;

//             // Update roles
//             _context.PersonRoles.RemoveRange(person.PersonRoles);

//             if (request.RoleIds.Any())
//             {
//                 var personRoles = request.RoleIds.Select(roleId => new PersonRole
//                 {
//                     PersonId = person.Id,
//                     RoleId = roleId
//                 }).ToList();

//                 _context.PersonRoles.AddRange(personRoles);
//             }

//             await _context.SaveChangesAsync(cancellationToken);

//             // Load roles for response
//             var roles = await _context.Roles
//                 .Where(r => request.RoleIds.Contains(r.Id) && !r.IsDeleted)
//                 .Select(r => new RoleBasicInfo
//                 {
//                     Id = r.Id,
//                     Name = r.Name,
//                     Description = r.Description
//                 })
//                 .ToListAsync(cancellationToken);

//             var userInfo = new UserInfo
//             {
//                 Id = person.Id,
//                 Username = person.Username,
//                 ImageProfile = person.ImageProfile,
//                 Email = person.Email,
//                 FirstName = person.FirstName,
//                 LastName = person.LastName,
//                 PhoneNumber = person.PhoneNumber,
//                 IsActive = person.IsActive,
//                 Roles = roles
//             };

//             return ApiResponse<UserInfo>.Ok(userInfo, "User updated successfully");
//         }
//     }
// }



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

namespace POS.Application.Features.User
{
    public record UpdateUserCommand : IRequest<ApiResponse<UserInfo>>
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<int> RoleIds { get; set; } = new();
    }

    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        private readonly IMyAppDbContext _context;

        public UpdateUserCommandValidator(IMyAppDbContext context)
        {
            _context = context;

            // ── UserId ────────────────────────────────────────────────────────
            RuleFor(x => x.UserId)
                .GreaterThan(0)
                    .WithMessage("A valid User ID is required.");

            // ── Username ──────────────────────────────────────────────────────
            RuleFor(x => x.Username)
                .NotEmpty()
                    .WithMessage("Username is required.")
                .MinimumLength(3)
                    .WithMessage("Username must be at least 3 characters.")
                .MaximumLength(50)
                    .WithMessage("Username must not exceed 50 characters.")
                .MustAsync(BeUniqueUsername)
                    .WithMessage("Username already exists. Please choose a different username.");

            // ── Email ─────────────────────────────────────────────────────────
            RuleFor(x => x.Email)
                .NotEmpty()
                    .WithMessage("Email is required.")
                .EmailAddress()
                    .WithMessage("Invalid email format. Example: example@mail.com")
                .MustAsync(BeUniqueEmail)
                    .WithMessage("Email already exists. Please use a different email address.");

            // ── Roles ─────────────────────────────────────────────────────────
            RuleFor(x => x.RoleIds)
                .NotNull()
                    .WithMessage("Role list is required.");

            RuleForEach(x => x.RoleIds)
                .MustAsync(RoleExists)
                    .WithMessage("One or more role IDs are invalid or have been deleted.");
        }

        // ── Helpers — exclude current user from uniqueness check ──────────────

        private async Task<bool> BeUniqueUsername(UpdateUserCommand cmd, string username, CancellationToken ct)
            => !await _context.Persons
                .AnyAsync(p => p.Username == username && p.Id != cmd.UserId && !p.IsDeleted, ct);

        private async Task<bool> BeUniqueEmail(UpdateUserCommand cmd, string email, CancellationToken ct)
            => !await _context.Persons
                .AnyAsync(p => p.Email == email && p.Id != cmd.UserId && !p.IsDeleted, ct);

        private async Task<bool> RoleExists(int roleId, CancellationToken ct)
            => await _context.Roles.AnyAsync(r => r.Id == roleId && !r.IsDeleted, ct);
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
                return ApiResponse<UserInfo>.NotFound("User not found.");
            var username = request.Username?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(username))
                return ApiResponse<UserInfo>.BadRequest("Username is required.");
            if (username.Length < 3)
                return ApiResponse<UserInfo>.BadRequest("Username must be at least 3 characters.");
            if (username.Length > 50)
                return ApiResponse<UserInfo>.BadRequest("Username must not exceed 50 characters.");

            var isDuplicateUsername = await _context.Persons
                .AnyAsync(p => p.Username == username
                            && p.Id != request.UserId
                            && !p.IsDeleted, cancellationToken);
            if (isDuplicateUsername)
                return ApiResponse<UserInfo>.BadRequest(
                    "Username already exists. Please choose a different username.");

            var email = request.Email?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(email))
                return ApiResponse<UserInfo>.BadRequest("Email is required.");
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return ApiResponse<UserInfo>.BadRequest(
                    "Invalid email format. Example: example@mail.com");

            var isDuplicateEmail = await _context.Persons
                .AnyAsync(p => p.Email == email
                            && p.Id != request.UserId
                            && !p.IsDeleted, cancellationToken);
            if (isDuplicateEmail)
                return ApiResponse<UserInfo>.BadRequest(
                    "Email already exists. Please use a different email address.");

            if (request.RoleIds.Any())
            {
                var validRoleIds = await _context.Roles
                    .Where(r => request.RoleIds.Contains(r.Id) && !r.IsDeleted)
                    .Select(r => r.Id)
                    .ToListAsync(cancellationToken);

                var invalidRoleIds = request.RoleIds.Except(validRoleIds).ToList();
                if (invalidRoleIds.Any())
                    return ApiResponse<UserInfo>.BadRequest(
                        $"Invalid role IDs: {string.Join(", ", invalidRoleIds)}. Please provide valid role IDs.");
            }

            person.Username = username;
            person.Email = email;
            person.IsActive = request.IsActive;

            _context.PersonRoles.RemoveRange(person.PersonRoles);

            if (request.RoleIds.Any())
            {
                _context.PersonRoles.AddRange(
                    request.RoleIds.Select(roleId => new PersonRole
                    {
                        PersonId = person.Id,
                        RoleId = roleId
                    })
                );
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
                when (ex.InnerException?.Message.Contains("23505") == true
                   || ex.InnerException?.Message.Contains("unique") == true)
            {
                var field = ex.InnerException.Message.Contains("email") ? "Email" : "Username";
                return ApiResponse<UserInfo>.BadRequest(
                    $"{field} already exists. Please use a different {field}.");
            }
            catch (DbUpdateException)
            {
                return ApiResponse<UserInfo>.BadRequest(
                    "Failed to update user. Please check your input and try again.");
            }

            var updatedUser = await _context.Persons
                .AsNoTracking()
                .Include(p => p.Staff)
                .Include(p => p.Customer)
                .Include(p => p.PersonRoles).ThenInclude(pr => pr.Role)
                .FirstOrDefaultAsync(p => p.Id == person.Id, cancellationToken);

            if (updatedUser == null)
                return ApiResponse<UserInfo>.BadRequest("Failed to retrieve updated user.");

            var userInfo = new UserInfo
            {
                Id = updatedUser.Id,
                Username = updatedUser.Username,
                Email = updatedUser.Email,
                IsActive = updatedUser.IsActive,
                Type = updatedUser.Type.ToString(),
                CreatedDate = updatedUser.CreatedDate,

                Roles = updatedUser.PersonRoles
                    .Where(pr => pr.Role != null && !pr.Role.IsDeleted)
                    .Select(pr => new RoleBasicInfo
                    {
                        Id = pr.Role.Id,
                        Name = pr.Role.Name,
                        Description = pr.Role.Description
                    }).ToList(),

                Staff = updatedUser.Type == PersonType.Staff && updatedUser.Staff != null
                    ? new StaffInfo
                    {
                        Id = updatedUser.Staff.Id,
                        FirstName = updatedUser.Staff.FirstName,
                        LastName = updatedUser.Staff.LastName,
                        PhoneNumber = updatedUser.Staff.PhoneNumber,
                        Position = updatedUser.Staff.Position,
                        Salary = updatedUser.Staff.Salary
                    }
                    : null,

                Customer = updatedUser.Type == PersonType.Customer && updatedUser.Customer != null
                    ? new CustomerInfo
                    {
                        Id = updatedUser.Customer.Id,
                        FirstName = updatedUser.Customer.FirstName,
                        LastName = updatedUser.Customer.LastName,
                        PhoneNumber = updatedUser.Customer.PhoneNumber,
                        TotalPoint = updatedUser.Customer.TotalPoint
                    }
                    : null
            };

            return ApiResponse<UserInfo>.Ok(userInfo, "User updated successfully.");
        }
    }
}