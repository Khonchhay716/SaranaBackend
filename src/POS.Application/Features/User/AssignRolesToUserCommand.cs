// POS.Application/Features/User/AssignRolesToUserCommand.cs
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
    public record AssignRolesToUserCommand : IRequest<ApiResponse>
    {
        public int UserId { get; set; }
        public List<int> RoleIds { get; set; } = new();
    }

    public class AssignRolesToUserCommandValidator : AbstractValidator<AssignRolesToUserCommand>
    {
        private readonly IMyAppDbContext _context;

        public AssignRolesToUserCommandValidator(IMyAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("Valid user ID is required")
                .MustAsync(UserExists).WithMessage("User not found");

            RuleFor(x => x.RoleIds)
                .NotNull().WithMessage("Role IDs list is required");

            RuleForEach(x => x.RoleIds)
                .MustAsync(RoleExists).WithMessage("One or more roles not found");
        }

        private async Task<bool> UserExists(int userId, CancellationToken cancellationToken)
        {
            return await _context.Persons.AnyAsync(p => p.Id == userId && !p.IsDeleted, cancellationToken);
        }

        private async Task<bool> RoleExists(int roleId, CancellationToken cancellationToken)
        {
            return await _context.Roles.AnyAsync(r => r.Id == roleId && !r.IsDeleted, cancellationToken);
        }
    }

    public class AssignRolesToUserCommandHandler : IRequestHandler<AssignRolesToUserCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;

        public AssignRolesToUserCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse> Handle(AssignRolesToUserCommand request, CancellationToken cancellationToken)
        {
            var person = await _context.Persons
                .Include(p => p.PersonRoles)
                .FirstOrDefaultAsync(p => p.Id == request.UserId && !p.IsDeleted, cancellationToken);

            if (person == null)
            {
                return ApiResponse.NotFound("User not found");
            }

            // Remove existing roles
            _context.PersonRoles.RemoveRange(person.PersonRoles);

            // Add new roles
            foreach (var roleId in request.RoleIds)
            {
                person.PersonRoles.Add(new PersonRole
                {
                    PersonId = person.Id,
                    RoleId = roleId
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok("Roles assigned successfully");
        }
    }
}