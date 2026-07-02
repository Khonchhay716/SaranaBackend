// POS.Application/Features/StockManagement/SerialStockListQuery.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using POS.Domain.Enums;

namespace POS.Application.Features.StockManagement
{
    public class SerialStockListQuery : PaginationRequest, IRequest<PaginatedResult<SerialStockInfo>>
    {
        public int? ProductId { get; set; }          // ✅ filter ដែល bong ត្រូវការ
        public SerialStatus? Status { get; set; }     // bonus: filter ដោយ status (Available/Sold/Lost/Damaged...)
        public string? Search { get; set; }            // bonus: search ដោយ SerialNo
    }

    public class SerialStockListQueryValidator : AbstractValidator<SerialStockListQuery>
    {
        public SerialStockListQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    public class SerialStockInfo
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SerialNo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset CreatedDate { get; set; }
    }

    public class SerialStockListQueryHandler : IRequestHandler<SerialStockListQuery, PaginatedResult<SerialStockInfo>>
    {
        private readonly IMyAppDbContext _context;

        public SerialStockListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<SerialStockInfo>> Handle(
            SerialStockListQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.SerialStocks
                .Where(s => !s.IsDeleted)
                .AsNoTracking();

            // ✅ Filter ដោយ ProductId
            if (request.ProductId.HasValue && request.ProductId.Value > 0)
                query = query.Where(s => s.ProductId == request.ProductId.Value);

            if (request.Status.HasValue)
                query = query.Where(s => s.Status == request.Status.Value);

            if (!string.IsNullOrWhiteSpace(request.Search))
                query = query.Where(s => s.SerialNo.Contains(request.Search));

            query = query.OrderByDescending(s => s.CreatedDate);

            var projected = query.Select(s => new SerialStockInfo
            {
                Id = s.Id,
                ProductId = s.ProductId,
                ProductName = s.Product.Name,
                SerialNo = s.SerialNo,
                Status = s.Status.ToString(),
                CreatedDate = s.CreatedDate
            });

            return await projected.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}