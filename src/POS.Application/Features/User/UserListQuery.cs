// POS.Application/Features/User/UserListQuery.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.User
{
    public class UserListQuery : PaginationRequest, IRequest<PaginatedResult<UserInfo>>
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
    }

    public class UserListQueryValidator : AbstractValidator<UserListQuery>
    {
        public UserListQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    public class UserListQueryHandler : IRequestHandler<UserListQuery, PaginatedResult<UserInfo>>
    {
        private readonly IMyAppDbContext _context;

        public UserListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<UserInfo>> Handle(UserListQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Persons
                .AsNoTracking()
                .Include(p => p.PersonRoles)
                    .ThenInclude(pr => pr.Role)
                .Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(p =>
                    p.Username.Contains(request.Search) ||
                    p.Email.Contains(request.Search) ||
                    p.FirstName.Contains(request.Search) ||
                    p.LastName.Contains(request.Search));
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(p => p.IsActive == request.IsActive.Value);
            }

            query = query.OrderByDescending(p => p.CreatedDate);

            var projectedQuery = query.Select(p => new UserInfo
            {
                Id = p.Id,
                Username = p.Username,
                ImageProfile= p.ImageProfile,
                Email = p.Email,
                FirstName = p.FirstName,
                LastName = p.LastName,
                PhoneNumber = p.PhoneNumber,
                IsActive = p.IsActive,
                CreatedDate = p.CreatedDate,
                Roles = p.PersonRoles
                    .Where(pr => !pr.Role.IsDeleted)
                    .Select(pr => new RoleBasicInfo
                    {
                        Id = pr.Role.Id,
                        Name = pr.Role.Name,
                        Description = pr.Role.Description
                    }).ToList()
            });

            return await projectedQuery.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}