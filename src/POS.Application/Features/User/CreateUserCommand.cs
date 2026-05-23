using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
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

    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        private readonly IMyAppDbContext _context;

        public CreateUserCommandValidator(IMyAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.Username)
                .NotEmpty()
                    .WithMessage("Username is required.")
                .MinimumLength(3)
                    .WithMessage("Username must be at least 3 characters.")
                .MaximumLength(50)
                    .WithMessage("Username must not exceed 50 characters.")
                .MustAsync(async (username, cancellationToken) => !await _context.Persons.AnyAsync(p => p.Username == username && !p.IsDeleted, cancellationToken))
                    .WithMessage("Username already exists. Please choose a different username.");

            RuleFor(x => x.Email)
                .NotEmpty()
                    .WithMessage("Email is required.")
                .EmailAddress()
                    .WithMessage("Invalid email format. Example: example@mail.com")
                .MustAsync(async (email, cancellationToken) => !await _context.Persons.AnyAsync(p => p.Email == email && !p.IsDeleted, cancellationToken))
                    .WithMessage("Email already exists. Please use a different email address.");

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

            RuleFor(x => x.RoleIds)
                .NotNull()
                    .WithMessage("Role list is required.");

            RuleForEach(x => x.RoleIds)
                .MustAsync(async (roleId, cancellationToken) => await _context.Roles.AnyAsync(r => r.Id == roleId && !r.IsDeleted, cancellationToken))
                    .WithMessage("One or more role IDs are invalid or have been deleted.");

            RuleFor(x => x)
                .Must(x => !(x.StaffId.HasValue && x.CustomerId.HasValue))
                    .WithMessage("Cannot assign both Staff and Customer to the same user. Please select only one.")
                    .OverridePropertyName("StaffId");

            When(x => x.StaffId.HasValue, () =>
            {
                RuleFor(x => x.StaffId!.Value)
                    .MustAsync(async (staffId, cancellationToken) => await _context.Staffs.AnyAsync(s => s.Id == staffId && !s.IsDeleted , cancellationToken))
                        .WithMessage("Staff not found. Please provide a valid Staff ID.")
                    .MustAsync(async (staffId, cancellationToken) => !await _context.Persons.AnyAsync(p => p.StaffId == staffId, cancellationToken))
                        .WithMessage("This Staff is already linked to another user.");
            });

            When(x => x.CustomerId.HasValue, () =>
            {
                RuleFor(x => x.CustomerId!.Value)
                    .MustAsync(async (customerId, cancellationToken) => await _context.Customers.AnyAsync(c => c.Id == customerId && !c.IsDeleted, cancellationToken))
                        .WithMessage("Customer not found. Please provide a valid Customer ID.")
                    .MustAsync(async (customerId, cancellationToken) => !await _context.Persons.AnyAsync(p => p.CustomerId == customerId, cancellationToken))
                        .WithMessage("This Customer is already linked to another user.");
            });
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
            // Determine PersonType
            var personType = request.StaffId.HasValue ? PersonType.Staff
                           : request.CustomerId.HasValue ? PersonType.Customer
                           : PersonType.None;

            // Hash password
            var hashedPassword = _passwordHasher.HashPassword(request.Password.Trim());

            var person = new PersonEntity
            {
                Username = request.Username.Trim(),
                Email = request.Email.Trim(),
                PasswordHash = hashedPassword,
                IsActive = request.IsActive,
                Type = personType,
                StaffId = request.StaffId,
                CustomerId = request.CustomerId,
                CreatedDate = DateTime.UtcNow
            };

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

            // Assign roles
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

            // Load Staff info
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
                        Salary = s.Salary,
                        ImageProfile = s.ImageProfile
                    })
                    .FirstOrDefaultAsync(cancellationToken);
            }

            // Load Customer info
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
                        TotalPoint = c.TotalPoint,
                        ImageProfile = c.ImageProfile
                    })
                    .FirstOrDefaultAsync(cancellationToken);
            }

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