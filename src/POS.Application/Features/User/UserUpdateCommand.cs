

using CoreAuthBackend.Client.Core.Models;
using Mapster;
using MediatR;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.User
{
    public record UserUpdateCommand : IRequest<ApiResponse<UserInfo>>
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserUpdateCommandHandler : IRequestHandler<UserUpdateCommand, ApiResponse<UserInfo>>
    {
        private readonly IMyAppDbContext _context;

        public UserUpdateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<UserInfo>> Handle(UserUpdateCommand filter, CancellationToken cancellationToken)
        {
            var userupdate = await _context.Users.FindAsync(filter.Id);
            if (userupdate == null) //// check condition if find id with user push come if the same it will not enter function this 
            {
                return ApiResponse<UserInfo>.NotFound(message: $"User with id {filter.Id} was not found !!!");
            }

            filter.Adapt(userupdate);
            userupdate.UpdatedDate = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<UserInfo>.Ok(message: $"User with id {filter.Id} was update successfully !!!");

        }
    }
}