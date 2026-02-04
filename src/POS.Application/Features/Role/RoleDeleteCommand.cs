using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Role
{
    public record RoleDeleteCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
    }

    public class RoleDeleteCommandHandler : IRequestHandler<RoleDeleteCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;

        public RoleDeleteCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse> Handle(RoleDeleteCommand request, CancellationToken cancellationToken)
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (role == null)
            {
                return ApiResponse.NotFound($"Role with id {request.Id} was not found");
            }

            // Check if role is assigned to any persons through PersonRoles
            var hasPersons = await _context.PersonRoles
                .AnyAsync(pr => pr.RoleId == request.Id, cancellationToken);

            if (hasPersons)
            {
                return ApiResponse.BadRequest("Cannot delete role because it is assigned to one or more persons");
            }

            role.IsDeleted = true;
            role.DeletedDate = DateTimeOffset.UtcNow;
            role.UpdatedDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Role with id {request.Id} deleted successfully");
        }
    }
}