// POS.Application/Features/SerialNumber/SerialNumberListQuery.cs
// ✅ Same namespace as commands — no extra using needed in controller
using FluentValidation;
using MediatR;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Application.Features.Product;

namespace POS.Application.Features.SerialNumber
{
    public class SerialNumberListQuery : PaginationRequest, IRequest<PaginatedResult<SerialNumberInfo>>
    {
        public int     ProductId { get; set; }
        public string? Search    { get; set; }
        public string? Status    { get; set; }  // "Available" | "Sold" | null = all
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate   { get; set; }
    }

    public class SerialNumberListQueryValidator : AbstractValidator<SerialNumberListQuery>
    {
        public SerialNumberListQueryValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0);
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    public class SerialNumberListQueryHandler : IRequestHandler<SerialNumberListQuery, PaginatedResult<SerialNumberInfo>>
    {
        private readonly IMyAppDbContext _context;

        public SerialNumberListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<SerialNumberInfo>> Handle(SerialNumberListQuery request, CancellationToken cancellationToken)
        {
            IQueryable<Domain.Entities.SerialNumber> query = _context.SerialNumbers
                .Where(s => s.ProductId == request.ProductId && !s.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.Search))
                query = query.Where(s => s.SerialNo.Contains(request.Search));

            if (!string.IsNullOrWhiteSpace(request.Status))
                query = query.Where(s => s.Status == request.Status);

            if (request.FromDate.HasValue)
                query = query.Where(s => s.CreatedDate >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(s => s.CreatedDate <= request.ToDate.Value);

            query = query.OrderByDescending(s => s.CreatedDate);

            var projected = query.Select(s => new SerialNumberInfo
            {
                Id          = s.Id,
                ProductId   = s.ProductId,
                SerialNo    = s.SerialNo,
                Status      = s.Status,
                Price       = s.Price,
                CostPrice   = s.CostPrice,
                CreatedDate = s.CreatedDate,
            });

            return await projected.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}