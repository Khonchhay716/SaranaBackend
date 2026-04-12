using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Leave
{
    public class LeaveTypeInfoLookup
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MaxDaysPerYear { get; set; }
    }
    public class LeaveTypeLookupListQuery : PaginationRequest, IRequest<PaginatedResult<LeaveTypeInfoLookup>>
    {
        public string? Search { get; set; }
    }

    public class LeaveTypeLookupListQueryHandler : IRequestHandler<LeaveTypeLookupListQuery, PaginatedResult<LeaveTypeInfoLookup>>
    {
        private readonly IMyAppDbContext _context;

        public LeaveTypeLookupListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<LeaveTypeInfoLookup>> Handle(
            LeaveTypeLookupListQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.LeaveTypes
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsActive); // ✅ active only for lookup

            if (!string.IsNullOrWhiteSpace(request.Search))
                query = query.Where(x => x.Name.ToLower().Contains(request.Search.ToLower()));

            query = query.OrderByDescending(x => x.CreatedDate);

            var projected = query.Select(x => new LeaveTypeInfoLookup
            {
                Id             = x.Id,
                Name           = x.Name,
                MaxDaysPerYear = x.MaxDaysPerYear,
            });

            return await projected.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }

}