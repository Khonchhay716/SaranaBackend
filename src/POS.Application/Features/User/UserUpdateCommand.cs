

using CoreAuthBackend.Client.Core.Models;
using Mapster;
using MediatR;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.User
{
    public record UserUpdateCommand : IRequest<ApiResponse<UserInfo>>
    {
        /// where write column for update 
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

        public async Task<ApiResponse<UserInfo>> Handle(UserUpdateCommand DTOupdate, CancellationToken cancellationToken)
        {
            var userupdate = await _context.Users.FindAsync(DTOupdate.Id);
            if (userupdate == null)
            {
                return ApiResponse<UserInfo>.NotFound(message: $"User with id {DTOupdate.Id} was not found !!!");
            }

            DTOupdate.Adapt(userupdate);
            userupdate.UpdatedDate = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            var info = userupdate.Adapt<UserInfo>();
            return ApiResponse<UserInfo>.Ok(info, message: $"User with id {DTOupdate.Id} was update successfully !!!");

        }
    }
}