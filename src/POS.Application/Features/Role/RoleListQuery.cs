using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Role
{
    public class RoleListQuery : PaginationRequest, IRequest<PaginatedResult<RoleInfos>>
    {
        public string? Search { get; set; }
    }

    public class RoleListQueryValidator : AbstractValidator<RoleListQuery>
    {
        public RoleListQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    public class RoleListQueryHandler : IRequestHandler<RoleListQuery, PaginatedResult<RoleInfos>>
    {
        private readonly IMyAppDbContext _context;

        public RoleListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<RoleInfos>> Handle(RoleListQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Roles
                .AsNoTracking()
                .Where(r => !r.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(r => r.Name.Contains(request.Search) || 
                                       r.Description.Contains(request.Search));
            }

            query = query.OrderByDescending(r => r.CreatedDate);

            var projectedQuery = query.Select(r => new RoleInfos
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
            });

            return await projectedQuery.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}