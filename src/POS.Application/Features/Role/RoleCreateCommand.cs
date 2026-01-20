using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;

namespace POS.Application.Features.Role
{
    public record RoleCreateCommand : IRequest<ApiResponse<RoleInfo>>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class RoleCreateCommandValidator : AbstractValidator<RoleCreateCommand>
    {
        private readonly IMyAppDbContext _context;
        public RoleCreateCommandValidator(IMyAppDbContext context)
        {
            _context = context;
            RuleFor(v => v.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MustAsync(BeUniqueName).WithMessage("The specified name already exists.");
        }

        public async Task<bool> BeUniqueName(string name, CancellationToken cancellationToken)
        {
            return await _context.Roles.AllAsync(r => r.Name != name, cancellationToken);
        }
    }

    public class RoleCreateCommandHandler : IRequestHandler<RoleCreateCommand, ApiResponse<RoleInfo>>
    {
        private readonly IMyAppDbContext _context;

        public RoleCreateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<RoleInfo>> Handle(RoleCreateCommand request, CancellationToken cancellationToken)
        {
            var role = new Domain.Entities.Role
            {
                Name = request.Name,
                Description = request.Description
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync(cancellationToken);

            var roleInfo = new RoleInfo
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                Permissions = new List<string>() // No permissions on creation
            };

            return ApiResponse<RoleInfo>.Created(roleInfo, "Role created successfully");
        }
    }
}