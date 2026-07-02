// StockReturnListQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using POS.Domain.Enums;
using System.Text.Json;

namespace POS.Application.Features.StockManagement.StockReturns
{
    public class StockReturnListQuery : PaginationRequest, IRequest<PaginatedResult<StockReturnInfo>>
    {
        public int? SupplierId { get; set; }
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
        public ReturnStatus? Status { get; set; }
        public string? Search { get; set; }
    }

    public class StockReturnListQueryHandler : IRequestHandler<StockReturnListQuery, PaginatedResult<StockReturnInfo>>
    {
        private readonly IMyAppDbContext _context;
        public StockReturnListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<StockReturnInfo>> Handle(StockReturnListQuery request, CancellationToken cancellationToken)
        {
            var query = _context.StockReturns
                .AsNoTracking()
                .Include(x => x.Supplier)
                .Include(x => x.Items).ThenInclude(x => x.Product)
                .AsQueryable();

            // Filters
            if (request.SupplierId.HasValue)
                query = query.Where(x => x.SupplierId == request.SupplierId.Value);

            if (request.Status.HasValue)
                query = query.Where(x => x.Status == request.Status.Value);

            if (!string.IsNullOrWhiteSpace(request.Search))
                query = query.Where(x => x.ReturnNo.Contains(request.Search));

            if (request.From.HasValue)
                query = query.Where(x => x.CreatedDate >= request.From.Value);

            if (request.To.HasValue)
                query = query.Where(x => x.CreatedDate < request.To.Value.AddDays(1));

            var totalCount = await query.CountAsync(cancellationToken);

            var dbItems = await query
                .OrderByDescending(x => x.CreatedDate)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var creatorIds = dbItems.Select(x => x.CreatedBy).Distinct().ToList();
            var creators = await _context.Persons
                .Where(p => creatorIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            var mappedItems = dbItems.Select(x => new StockReturnInfo
            {
                Id = x.Id,
                ReturnNo = x.ReturnNo,
                SupplierId = x.SupplierId,
                SupplierName = x.Supplier.Name,
                Note = x.Note,
                TotalAmount = x.TotalAmount,
                Status = x.Status.ToString(),        
                CreatedDate = x.CreatedDate,
                CreatedBy = creators.TryGetValue(x.CreatedBy ?? 0, out var creator)
                    ? new TypeNamebase { Id = creator.Id, Name = creator.Username }
                    : null,
                Items = x.Items.Select(i => new StockReturnItemInfo
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    ProductCode = i.Product.Code,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice, 
                    Reason = i.Reason.ToString(),
                    Note = i.Note,
                    SerialNumbers = !string.IsNullOrWhiteSpace(i.SerialNumbers)
                        ? JsonSerializer.Deserialize<List<string>>(i.SerialNumbers)
                        : null
                }).ToList()
            }).ToList();

            return new PaginatedResult<StockReturnInfo>
            {
                data = mappedItems,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}