// // POS.Application/Features/User/CreateUserCommand.cs
// using FluentValidation;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Dto;
// using POS.Application.Common.Interfaces;
// using POS.Domain.Entities;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text.RegularExpressions;
// using System.Threading;
// using System.Threading.Tasks;
// using PersonEntity = POS.Domain.Entities.Person;

// namespace POS.Application.Features.User
// {
//     public record CreateUserCommand : IRequest<ApiResponse<UserInfo>>
//     {
//         public string Username { get; set; } = string.Empty;
//         public string ImageProfile { get; set; } = string.Empty;
//         public string Email { get; set; } = string.Empty;
//         public string Password { get; set; } = string.Empty;
//         public string FirstName { get; set; } = string.Empty;
//         public string LastName { get; set; } = string.Empty;
//         public string? PhoneNumber { get; set; }
//         public bool IsActive { get; set; } = true;
//         public List<int> RoleIds { get; set; } = new();
//     }

//     public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
//     {
//         private readonly IMyAppDbContext _context;

//         public CreateUserCommandValidator(IMyAppDbContext context)
//         {
//             _context = context;

//             RuleFor(x => x.Username)
//                 .NotEmpty().WithMessage("Username is required")
//                 .MinimumLength(3).WithMessage("Username must be at least 3 characters")
//                 .MaximumLength(50).WithMessage("Username must not exceed 50 characters")
//                 .MustAsync(BeUniqueUsername).WithMessage("Username already exists");

//             RuleFor(x => x.Email)
//                 .NotEmpty().WithMessage("Email is required")
//                 .EmailAddress().WithMessage("Invalid email format")
//                 .MustAsync(BeUniqueEmail).WithMessage("Email already exists");

//             RuleFor(x => x.Password)
//                 .NotEmpty().WithMessage("Password is required")
//                 .MinimumLength(8).WithMessage("Password must be at least 8 characters");

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

//         private async Task<bool> BeUniqueUsername(string username, CancellationToken cancellationToken)
//         {
//             return !await _context.Persons.AnyAsync(p => p.Username == username, cancellationToken);
//         }

//         private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
//         {
//             return !await _context.Persons.AnyAsync(p => p.Email == email, cancellationToken);
//         }

//         private async Task<bool> RoleExists(int roleId, CancellationToken cancellationToken)
//         {
//             return await _context.Roles.AnyAsync(r => r.Id == roleId && !r.IsDeleted, cancellationToken);
//         }
//     }

//     public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, ApiResponse<UserInfo>>
//     {
//         private readonly IMyAppDbContext _context;
//         private readonly IPasswordHasher _passwordHasher;

//         public CreateUserCommandHandler(IMyAppDbContext context, IPasswordHasher passwordHasher)
//         {
//             _context = context;
//             _passwordHasher = passwordHasher;
//         }

//         public async Task<ApiResponse<UserInfo>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
//         {
//             if (string.IsNullOrWhiteSpace(request.Username.Trim()))
//             {
//                 return ApiResponse<UserInfo>.BadRequest("Username is required");
//             }
//             if (request.Username.Length < 3)
//             {
//                 return ApiResponse<UserInfo>.BadRequest("Username must enter at least 3 character");
//             }
//             var isDuplicateUsername = await _context.Persons.AnyAsync(p => p.Username == request.Username);
//             if (isDuplicateUsername)
//             {
//                 return ApiResponse<UserInfo>.BadRequest("Username already exists");
//             }
//             var isDuplicateEmail = await _context.Persons.AnyAsync(p => p.Email == request.Email && p.IsDeleted == false);
//             if (isDuplicateEmail)
//             {
//                 return ApiResponse<UserInfo>.BadRequest("Email already exists");
//             }
//             var password = request.Password?.Trim() ?? "";

//             if (string.IsNullOrWhiteSpace(password))
//             {
//                 return ApiResponse<UserInfo>.BadRequest("Password is required");
//             }

//             if (password.Length < 8)
//             {
//                 return ApiResponse<UserInfo>.BadRequest("Password must be at least 8 characters");
//             }

//             if (!Regex.IsMatch(password, "[A-Z]"))
//             {
//                 return ApiResponse<UserInfo>.BadRequest("Password must contain at least one uppercase letter");
//             }

//             if (!Regex.IsMatch(password, "[a-z]"))
//             {
//                 return ApiResponse<UserInfo>.BadRequest("Password must contain at least one lowercase letter");
//             }

//             if (!Regex.IsMatch(password, @"\d"))
//             {
//                 return ApiResponse<UserInfo>.BadRequest("Password must contain at least one number");
//             }

//             if (!Regex.IsMatch(password, @"[!@#$%^&*]"))
//             {
//                 return ApiResponse<UserInfo>.BadRequest("Password must contain at least one special character (!@#$%^&*)");
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

//             var hashedPassword = _passwordHasher.HashPassword(request.Password);

//             var person = new PersonEntity
//             {
//                 Username = request.Username,
//                 ImageProfile = request.ImageProfile,
//                 Email = request.Email,
//                 PasswordHash = hashedPassword,
//                 FirstName = request.FirstName,
//                 LastName = request.LastName,
//                 PhoneNumber = request.PhoneNumber,
//                 IsActive = request.IsActive,
//                 CreatedDate = DateTime.UtcNow
//             };

//             _context.Persons.Add(person);
//             await _context.SaveChangesAsync(cancellationToken);

//             // Assign roles
//             if (request.RoleIds.Any())
//             {
//                 var personRoles = request.RoleIds.Select(roleId => new PersonRole
//                 {
//                     PersonId = person.Id,
//                     RoleId = roleId
//                 }).ToList();

//                 _context.PersonRoles.AddRange(personRoles);
//                 await _context.SaveChangesAsync(cancellationToken);
//             }

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

//             return ApiResponse<UserInfo>.Created(userInfo, "User created successfully");
//         }
//     }
// }


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
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public List<int> RoleIds { get; set; } = new();
        public int? StaffId { get; set; }
        public int? CustomerId { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Validator
    // ─────────────────────────────────────────────────────────────────────────
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        private readonly IMyAppDbContext _context;

        public CreateUserCommandValidator(IMyAppDbContext context)
        {
            _context = context;

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

            // ── Password ──────────────────────────────────────────────────────
            RuleFor(x => x.Password)
                .NotEmpty()
                    .WithMessage("Password is required.")
                .MinimumLength(8)
                    .WithMessage("Password must be at least 8 characters.")
                .Matches("[A-Z]")
                    .WithMessage("Password must contain at least one uppercase letter (A-Z).")
                .Matches("[a-z]")
                    .WithMessage("Password must contain at least one lowercase letter (a-z).")
                .Matches(@"\d")
                    .WithMessage("Password must contain at least one number (0-9).")
                .Matches(@"[!@#$%^&*]")
                    .WithMessage("Password must contain at least one special character (!@#$%^&*).");

            // ── Roles ─────────────────────────────────────────────────────────
            RuleFor(x => x.RoleIds)
                .NotNull()
                    .WithMessage("Role list is required.");

            RuleForEach(x => x.RoleIds)
                .MustAsync(RoleExists)
                    .WithMessage("One or more role IDs are invalid or have been deleted.");

            // ── Staff / Customer mutual exclusion ─────────────────────────────
            RuleFor(x => x)
                .Must(x => !(x.StaffId.HasValue && x.CustomerId.HasValue))
                    .WithMessage("Cannot assign both Staff and Customer to the same user. Please select only one.")
                    .OverridePropertyName("StaffId");

            // ── StaffId ───────────────────────────────────────────────────────
            When(x => x.StaffId.HasValue, () =>
            {
                RuleFor(x => x.StaffId!.Value)
                    .MustAsync(StaffExists)
                        .WithMessage("Staff not found. Please provide a valid Staff ID.")
                    .MustAsync(StaffNotLinked)
                        .WithMessage("This Staff is already linked to another user.");
            });

            // ── CustomerId ────────────────────────────────────────────────────
            When(x => x.CustomerId.HasValue, () =>
            {
                RuleFor(x => x.CustomerId!.Value)
                    .MustAsync(CustomerExists)
                        .WithMessage("Customer not found. Please provide a valid Customer ID.")
                    .MustAsync(CustomerNotLinked)
                        .WithMessage("This Customer is already linked to another user.");
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task<bool> BeUniqueUsername(string username, CancellationToken ct)
            => !await _context.Persons.AnyAsync(p => p.Username == username && !p.IsDeleted, ct);

        private async Task<bool> BeUniqueEmail(string email, CancellationToken ct)
            => !await _context.Persons.AnyAsync(p => p.Email == email && !p.IsDeleted, ct);

        private async Task<bool> RoleExists(int roleId, CancellationToken ct)
            => await _context.Roles.AnyAsync(r => r.Id == roleId && !r.IsDeleted, ct);

        private async Task<bool> StaffExists(int staffId, CancellationToken ct)
            => await _context.Staffs.AnyAsync(s => s.Id == staffId && !s.IsDeleted, ct);

        private async Task<bool> StaffNotLinked(int staffId, CancellationToken ct)
            => !await _context.Persons.AnyAsync(p => p.StaffId == staffId, ct);

        private async Task<bool> CustomerExists(int customerId, CancellationToken ct)
            => await _context.Customers.AnyAsync(c => c.Id == customerId && !c.IsDeleted, ct);

        private async Task<bool> CustomerNotLinked(int customerId, CancellationToken ct)
            => !await _context.Persons.AnyAsync(p => p.CustomerId == customerId, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Handler
    // ─────────────────────────────────────────────────────────────────────────
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
            // ── 1. Username: empty + length ───────────────────────────────────
            var username = request.Username?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(username))
                return ApiResponse<UserInfo>.BadRequest("Username is required.");
            if (username.Length < 3)
                return ApiResponse<UserInfo>.BadRequest("Username must be at least 3 characters.");
            if (username.Length > 50)
                return ApiResponse<UserInfo>.BadRequest("Username must not exceed 50 characters.");

            // ── 2. Username: uniqueness ───────────────────────────────────────
            var isDuplicateUsername = await _context.Persons
                .AnyAsync(p => p.Username == username && !p.IsDeleted, cancellationToken);
            if (isDuplicateUsername)
                return ApiResponse<UserInfo>.BadRequest("Username already exists. Please choose a different username.");

            // ── 3. Email: empty + format ──────────────────────────────────────
            var email = request.Email?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(email))
                return ApiResponse<UserInfo>.BadRequest("Email is required.");
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return ApiResponse<UserInfo>.BadRequest("Invalid email format. Example: example@mail.com");

            // ── 4. Email: uniqueness ──────────────────────────────────────────
            var isDuplicateEmail = await _context.Persons
                .AnyAsync(p => p.Email == email && !p.IsDeleted, cancellationToken);
            if (isDuplicateEmail)
                return ApiResponse<UserInfo>.BadRequest("Email already exists. Please use a different email address.");

            // ── 5. Password: all complexity rules ─────────────────────────────
            var password = request.Password?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(password))
                return ApiResponse<UserInfo>.BadRequest("Password is required.");
            if (password.Length < 8)
                return ApiResponse<UserInfo>.BadRequest("Password must be at least 8 characters.");
            if (!Regex.IsMatch(password, "[A-Z]"))
                return ApiResponse<UserInfo>.BadRequest("Password must contain at least one uppercase letter (A-Z).");
            if (!Regex.IsMatch(password, "[a-z]"))
                return ApiResponse<UserInfo>.BadRequest("Password must contain at least one lowercase letter (a-z).");
            if (!Regex.IsMatch(password, @"\d"))
                return ApiResponse<UserInfo>.BadRequest("Password must contain at least one number (0-9).");
            if (!Regex.IsMatch(password, @"[!@#$%^&*]"))
                return ApiResponse<UserInfo>.BadRequest("Password must contain at least one special character (!@#$%^&*).");

            // ── 6. Cannot pick both Staff and Customer ────────────────────────
            if (request.StaffId.HasValue && request.CustomerId.HasValue)
                return ApiResponse<UserInfo>.BadRequest("Cannot assign both Staff and Customer to the same user. Please select only one.");

            // ── 7. Roles: validate all exist ──────────────────────────────────
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

            // ── 8. StaffId: exists + not linked ──────────────────────────────
            if (request.StaffId.HasValue)
            {
                var staffExists = await _context.Staffs
                    .AnyAsync(s => s.Id == request.StaffId.Value && !s.IsDeleted, cancellationToken);
                if (!staffExists)
                    return ApiResponse<UserInfo>.BadRequest(
                        $"Staff with ID {request.StaffId.Value} not found. Please provide a valid Staff ID.");

                var staffLinked = await _context.Persons
                    .AnyAsync(p => p.StaffId == request.StaffId.Value, cancellationToken);
                if (staffLinked)
                    return ApiResponse<UserInfo>.BadRequest(
                        $"Staff with ID {request.StaffId.Value} is already linked to another user.");
            }

            // ── 9. CustomerId: exists + not linked ────────────────────────────
            if (request.CustomerId.HasValue)
            {
                var customerExists = await _context.Customers
                    .AnyAsync(c => c.Id == request.CustomerId.Value && !c.IsDeleted, cancellationToken);
                if (!customerExists)
                    return ApiResponse<UserInfo>.BadRequest(
                        $"Customer with ID {request.CustomerId.Value} not found. Please provide a valid Customer ID.");

                var customerLinked = await _context.Persons
                    .AnyAsync(p => p.CustomerId == request.CustomerId.Value, cancellationToken);
                if (customerLinked)
                    return ApiResponse<UserInfo>.BadRequest(
                        $"Customer with ID {request.CustomerId.Value} is already linked to another user.");
            }

            // ── 10. Determine PersonType ──────────────────────────────────────
            var personType = request.StaffId.HasValue ? PersonType.Staff
                           : request.CustomerId.HasValue ? PersonType.Customer
                           : PersonType.None;

            // ── 11. Hash password + create Person ─────────────────────────────
            var hashedPassword = _passwordHasher.HashPassword(password);

            var person = new PersonEntity
            {
                Username = username,
                Email = email,
                PasswordHash = hashedPassword,
                IsActive = request.IsActive,
                Type = personType,
                StaffId = request.StaffId,
                CustomerId = request.CustomerId,
                CreatedDate = DateTime.UtcNow
            };

            // ── 12. Save Person — catch any FK violation ──────────────────────
            try
            {
                _context.Persons.Add(person);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
                when (ex.InnerException?.Message.Contains("23503") == true
                   || ex.InnerException?.Message.Contains("foreign key") == true)
            {
                var field = ex.InnerException.Message.Contains("CustomerId") ? "Customer" : "Staff";
                return ApiResponse<UserInfo>.BadRequest(
                    $"{field} ID does not exist. Please provide a valid {field} ID.");
            }
            catch (DbUpdateException)
            {
                return ApiResponse<UserInfo>.BadRequest(
                    "Failed to save user. Please check your input and try again.");
            }

            // ── 13. Assign roles ──────────────────────────────────────────────
            if (request.RoleIds.Any())
            {
                var personRoles = request.RoleIds
                    .Select(roleId => new PersonRole
                    {
                        PersonId = person.Id,
                        RoleId = roleId
                    }).ToList();

                _context.PersonRoles.AddRange(personRoles);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // ── 14. Load roles for response ───────────────────────────────────
            var roles = await _context.Roles
                .Where(r => request.RoleIds.Contains(r.Id) && !r.IsDeleted)
                .Select(r => new RoleBasicInfo
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description
                })
                .ToListAsync(cancellationToken);

            // ── 15. Load Staff info ───────────────────────────────────────────
            StaffInfo? staffInfo = null;
            if (request.StaffId.HasValue)
            {
                staffInfo = await _context.Staffs
                    .Where(s => s.Id == request.StaffId.Value)
                    .Select(s => new StaffInfo
                    {
                        Id = s.Id,
                        FirstName = s.FirstName,
                        LastName = s.LastName,
                        PhoneNumber = s.PhoneNumber,
                        Position = s.Position,
                        Salary = s.Salary
                    })
                    .FirstOrDefaultAsync(cancellationToken);
            }

            // ── 16. Load Customer info ────────────────────────────────────────
            CustomerInfo? customerInfo = null;
            if (request.CustomerId.HasValue)
            {
                customerInfo = await _context.Customers
                    .Where(c => c.Id == request.CustomerId.Value)
                    .Select(c => new CustomerInfo
                    {
                        Id = c.Id,
                        FirstName = c.FirstName,
                        LastName = c.LastName,
                        PhoneNumber = c.PhoneNumber,
                        TotalPoint = c.TotalPoint
                    })
                    .FirstOrDefaultAsync(cancellationToken);
            }

            // ── 17. Build + return response ───────────────────────────────────
            var userInfo = new UserInfo
            {
                Id = person.Id,
                Username = person.Username,
                Email = person.Email,
                IsActive = person.IsActive,
                Type = personType.ToString(),
                CreatedDate = person.CreatedDate,
                Roles = roles,
                Staff = staffInfo,
                Customer = customerInfo
            };

            return ApiResponse<UserInfo>.Created(userInfo, "User created successfully.");
        }
    }
}