

using CoreAuthBackend.Client.Core.Models;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.User
{
    public record UserQuery : IRequest<ApiResponse<UserInfo>>
    {
        public int Id { get; set; }
    }

    public class UserQueryHandler : IRequestHandler<UserQuery, ApiResponse<UserInfo>>
    {
        private readonly IMyAppDbContext _context;

        public UserQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<UserInfo>> Handle(UserQuery DtogetbyId, CancellationToken cancellationToken)
        {
            var user = await _context.Users
            .AsTracking()
            .Where(x => x.IsDeleted == false && x.Id == DtogetbyId.Id)
            .ProjectToType<UserInfo>()
            .FirstOrDefaultAsync();

            if (user == null)
            {
                return ApiResponse<UserInfo>.NotFound(message: $"User with id {DtogetbyId.Id} was not found !");
            }

            // var info = user.Adapt<UserInfo>();  /// code this work the same ProjectToType

            return ApiResponse<UserInfo>.Ok(user, message: "get data user is successfully ");
        }
    }
}