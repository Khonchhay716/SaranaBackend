using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Features.Permission;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Role
{
    public record AssignPermissionsToRoleCommand : IRequest<ApiResponse>
    {
        public int RoleId { get; set; }
        public List<string> Permissions { get; set; } = new();
    }

    public class AssignPermissionsToRoleCommandValidator : AbstractValidator<AssignPermissionsToRoleCommand>
    {
        private readonly IMyAppDbContext _context;
        
        public AssignPermissionsToRoleCommandValidator(IMyAppDbContext context)
        {
            _context = context;
            
            RuleFor(v => v.RoleId)
                .NotEmpty().WithMessage("RoleId is required.")
                .MustAsync(RoleExists).WithMessage("Role not found.");

            RuleFor(v => v.Permissions)
                .NotNull().WithMessage("Permissions list is required.");

            RuleForEach(v => v.Permissions)
                .Must(PermissionExists).WithMessage("One or more permissions are invalid.");
        }

        private async Task<bool> RoleExists(int roleId, CancellationToken cancellationToken)
        {
            return await _context.Roles.AnyAsync(r => r.Id == roleId && !r.IsDeleted, cancellationToken);
        }

        private bool PermissionExists(string permissionName)
        {
            return PermissionData.Permissions.Any(p => p.Name == permissionName);
        }
    }

    public class AssignPermissionsToRoleCommandHandler : IRequestHandler<AssignPermissionsToRoleCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;

        public AssignPermissionsToRoleCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse> Handle(AssignPermissionsToRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == request.RoleId && !r.IsDeleted, cancellationToken);

            if (role == null)
            {
                return ApiResponse.NotFound($"Role with id {request.RoleId} was not found");
            }

            // Remove existing permissions
            _context.RolePermissions.RemoveRange(role.RolePermissions);

            // Add new permissions
            foreach (var permissionName in request.Permissions)
            {
                role.RolePermissions.Add(new Domain.Entities.RolePermission
                {
                    RoleId = role.Id,
                    PermissionName = permissionName
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Permissions updated successfully for role '{role.Name}'");
        }
    }
}