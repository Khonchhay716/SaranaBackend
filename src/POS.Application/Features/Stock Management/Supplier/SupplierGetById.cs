using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.StockManagement.Suppliers
{
    public record SupplierGetByIdQuery : IRequest<ApiResponse<SupplierInfo>>
    {
        public int Id { get; set; }
    }

    public class SupplierGetByIdQueryHandler : IRequestHandler<SupplierGetByIdQuery, ApiResponse<SupplierInfo>>
    {
        private readonly IMyAppDbContext _context;
        public SupplierGetByIdQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<SupplierInfo>> Handle(SupplierGetByIdQuery request, CancellationToken cancellationToken)
        {
            var supplier = await _context.Suppliers
                .AsNoTracking()
                .Where(x => x.Id == request.Id && !x.IsDeleted)
                .Select(x => new SupplierInfo
                {
                    Id = x.Id,
                    Name = x.Name,
                    Phone = x.Phone,
                    Email = x.Email,
                    Address = x.Address,
                    CreatedDate = x.CreatedDate
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (supplier == null)
                return ApiResponse<SupplierInfo>.NotFound($"Supplier with Id {request.Id} was not found.");

            return ApiResponse<SupplierInfo>.Ok(supplier);
        }
    }
}