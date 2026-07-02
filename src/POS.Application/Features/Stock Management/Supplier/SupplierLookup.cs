using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.StockManagement.Suppliers
{
    public class SupplierLookupQuery : PaginationRequest, IRequest<PaginatedResult<SupplierLookup>>
    {
        public string? Search { get; set; }
    }

    public class SupplierLookupQueryHandler : IRequestHandler<SupplierLookupQuery, PaginatedResult<SupplierLookup>>
    {
        private readonly IMyAppDbContext _context;
        public SupplierLookupQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<SupplierLookup>> Handle(SupplierLookupQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Suppliers
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(x =>
                    x.Name.Contains(request.Search) ||
                    (x.Phone ?? "").Contains(request.Search) ||
                    (x.Email ?? "").Contains(request.Search));
            }

            var result = query.Select(x => new SupplierLookup
            {
                Id = x.Id,
                Name = x.Name,
            });

            return await result.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}