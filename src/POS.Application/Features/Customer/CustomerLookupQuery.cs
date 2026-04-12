using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Customer
{
    public class CustomerInfoLookup
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int TotalPoint { get; set; }
    }

    public class CustomerLookupListQuery : PaginationRequest, IRequest<PaginatedResult<CustomerInfoLookup>>
    {
        public string? Search { get; set; }
    }

    public class CustomerLookupListQueryHandler : IRequestHandler<CustomerLookupListQuery, PaginatedResult<CustomerInfoLookup>>
    {
        private readonly IMyAppDbContext _context;

        public CustomerLookupListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<CustomerInfoLookup>> Handle(
            CustomerLookupListQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Customers
                .Where(c => !c.IsDeleted && c.Status == true)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(c =>
                    c.FirstName.Contains(request.Search) ||
                    c.LastName.Contains(request.Search) ||
                    c.PhoneNumber.Contains(request.Search));
            }

            query = query.OrderByDescending(c => c.Id);

            var projected = query.Select(c => new CustomerInfoLookup
            {
                Id = c.Id,
                FullName = c.FirstName + " " + c.LastName,
                PhoneNumber = c.PhoneNumber,
                TotalPoint = c.TotalPoint,
            });

            return await projected.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}