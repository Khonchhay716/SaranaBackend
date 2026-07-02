using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.StockManagement.Suppliers
{
    public class SupplierListQuery : PaginationRequest, IRequest<PaginatedResult<SupplierInfo>>
    {
        public string? Search { get; set; }
    }

    public class SupplierListQueryHandler : IRequestHandler<SupplierListQuery, PaginatedResult<SupplierInfo>>
    {
        private readonly IMyAppDbContext _context;
        public SupplierListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<SupplierInfo>> Handle(SupplierListQuery request, CancellationToken cancellationToken)
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

            var result = query.Select(x => new SupplierInfo
            {
                Id = x.Id,
                Name = x.Name,
                Phone = x.Phone,
                Email = x.Email,
                Address = x.Address,
                CreatedDate = x.CreatedDate
            });

            return await result.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}