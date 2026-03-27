// POS.Application/Features/Branch/BranchLookupQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Branch
{
    public class BranchInfoLookup
    {
        public int    Id         { get; set; }
        public string BranchName { get; set; } = string.Empty;
    }

    public class BranchLookupListQuery : PaginationRequest, IRequest<PaginatedResult<BranchInfoLookup>>
    {
        public string? Search { get; set; }
    }

    public class BranchLookupListQueryHandler : IRequestHandler<BranchLookupListQuery, PaginatedResult<BranchInfoLookup>>
    {
        private readonly IMyAppDbContext _context;

        public BranchLookupListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<BranchInfoLookup>> Handle(BranchLookupListQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Branches
                .Where(b => !b.IsDeleted && b.Status == "Active")  // ✅ hardcoded Active only
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(b =>
                    b.BranchName.Contains(request.Search) ||
                    (b.Description != null && b.Description.Contains(request.Search)));
            }

            query = query.OrderByDescending(b => b.Id);

            var projectedQuery = query.Select(b => new BranchInfoLookup
            {
                Id         = b.Id,
                BranchName = b.BranchName,
            });

            return await projectedQuery.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}