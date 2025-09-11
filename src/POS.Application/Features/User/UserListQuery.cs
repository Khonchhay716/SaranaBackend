

using CoreAuthBackend.Client.Core.Extensions;
using CoreAuthBackend.Client.Core.Models;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.User
{
    public class UserListquery : PaginationRequest, IRequest<PaginatedResult<UserInfo>>
    {
        ///// where for write filter or ..... 
    }

    public class UserListQueryHandler : IRequestHandler<UserListquery, PaginatedResult<UserInfo>>
    {
        private readonly IMyAppDbContext _context;

        public UserListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<UserInfo>> Handle(UserListquery Allfiter, CancellationToken cancellationToken)
        {
            var query = _context.Users.AsNoTracking().Where(x=> x.IsDeleted == false);
            var q = query.ProjectToType<UserInfo>();

            return await q.ToPaginatedResultAsync(Allfiter.Page, Allfiter.PageSize);
        }
    }
}