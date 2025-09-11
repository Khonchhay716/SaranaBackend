
using CoreAuthBackend.Client.Core.Models;
using MediatR;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.User
{
    public record UserDeleteCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
    }

    public class UserDeleteCommandhandler : IRequestHandler<UserDeleteCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;

        public UserDeleteCommandhandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse> Handle(UserDeleteCommand filter, CancellationToken cancellationToken)
        {
            //// find data in table 
            var user = await _context.Users.FindAsync(filter.Id);
            if (user == null)
            {
                return null;
            }
            user.IsDeleted = true; //// update it to IsDelete is true 
            user.DeletedDate = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"User with id {filter.Id} Delete successfully !!!");
        }
    }
}