

using CoreAuthBackend.Client.Core.Extensions;
using CoreAuthBackend.Client.Core.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.User
{
    public class UserListquery : PaginationRequest, IRequest<PaginatedResult<UserInfo>>
    {
        ///// where for write filter or  .......
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
            var query = _context.Users
            .AsNoTracking()   //// use it for ready only 
            .Where(x => x.IsDeleted == false)  /// use it for get data that have column is false only 
            .OrderByDescending(x => x.Id)  //// sort data big to small 
            .ProjectToType<UserInfo>();  //// format data first to response 

            return await query.ToPaginatedResultAsync(Allfiter.Page, Allfiter.PageSize);
        }
    }
}