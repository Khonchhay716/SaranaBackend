using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;

namespace POS.Application.Features.Role
{
    public record RoleUpdateCommand : IRequest<ApiResponse<RoleInfos>>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // ---------------- VALIDATOR ----------------
    public class RoleUpdateCommandValidator : AbstractValidator<RoleUpdateCommand>
    {
        public RoleUpdateCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0);

            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Description)
                .MaximumLength(250);
        }
    }

    // ---------------- HANDLER ----------------
    public class RoleUpdateCommandHandler
        : IRequestHandler<RoleUpdateCommand, ApiResponse<RoleInfos>>
    {
        private readonly IMyAppDbContext _context;

        public RoleUpdateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<RoleInfos>> Handle(
            RoleUpdateCommand request,
            CancellationToken cancellationToken)
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(
                    x => x.Id == request.Id && !x.IsDeleted,
                    cancellationToken);

            if (role == null)
            {
                return ApiResponse<RoleInfos>.NotFound(
                    $"Role with id {request.Id} not found");
            }

            // Map request → entity
            request.Adapt(role);

            role.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Map entity → DTO
            var roleInfo = role.Adapt<RoleInfos>();

            return ApiResponse<RoleInfos>.Ok(
                roleInfo,
                $"Role with id {request.Id} was updated successfully"
            );
        }
    }
}
