

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

        public async Task<ApiResponse<UserInfo>> Handle(UserQuery filter, CancellationToken cancellationToken)
        {
            // var users = await _context.Users.AsNoTracking().ToListAsync(); /// how to get data all i table 
            var user = await _context.Users.AsTracking().Where(x => x.IsDeleted == false && x.Id == filter.Id).FirstOrDefaultAsync(cancellationToken);
            if (user == null)
            {
                return ApiResponse<UserInfo>.NotFound(message: $"User with id {filter.Id} was not found !");
            }

            return ApiResponse<UserInfo>.Ok(user.Adapt<UserInfo>(), message: "get data user is successfully ");           
        }
    }
}