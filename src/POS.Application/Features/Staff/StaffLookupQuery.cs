using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Staff
{
    public class StaffInfoLookup
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
    }

    public class StaffLookupListQuery : PaginationRequest, IRequest<PaginatedResult<StaffInfoLookup>>
    {
        public string? Search { get; set; }
    }

    public class StaffLookupListQueryHandler : IRequestHandler<StaffLookupListQuery, PaginatedResult<StaffInfoLookup>>
    {
        private readonly IMyAppDbContext _context;

        public StaffLookupListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<StaffInfoLookup>> Handle(
            StaffLookupListQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Staffs
                .Where(s => !s.IsDeleted)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(s =>
                    s.FirstName.Contains(request.Search) ||
                    s.LastName.Contains(request.Search));
            }

            query = query.OrderByDescending(s => s.Id);

            var projected = query.Select(s => new StaffInfoLookup
            {
                Id = s.Id,
                FullName = s.FirstName + " " + s.LastName,
                Position = s.Position,
            });

            return await projected.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}