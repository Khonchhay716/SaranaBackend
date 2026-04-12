// POS.Application/Features/SerialNumber/SerialNumberListQuery.cs
using FluentValidation;
using MediatR;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Application.Features.Product;

namespace POS.Application.Features.SerialNumber
{
    // 1 = Available, 2 = Sold
    public enum SerialNumberStatus
    {
        Available = 1,
        Sold = 2,
    }

    public class SerialNumberListQuery : PaginationRequest, IRequest<PaginatedResult<SerialNumberInfo>>
    {
        public int? ProductId { get; set; }   // optional — null = all products
        public string? Search { get; set; }
        public SerialNumberStatus? Status { get; set; }   // 1=Available, 2=Sold, null=all
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
    }

    public class SerialNumberListQueryValidator : AbstractValidator<SerialNumberListQuery>
    {
        public SerialNumberListQueryValidator()
        {
            // ProductId is optional, but if provided must be > 0
            RuleFor(x => x.ProductId)
                .GreaterThan(0)
                .When(x => x.ProductId.HasValue)
                .WithMessage("ProductId must be greater than 0 when provided.");

            // Status if provided must be a valid enum value (1 or 2)
            RuleFor(x => x.Status)
                .IsInEnum()
                .When(x => x.Status.HasValue)
                .WithMessage("Status must be 1 (Available) or 2 (Sold).");

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

        public async Task<PaginatedResult<SerialNumberInfo>> Handle(
            SerialNumberListQuery request,
            CancellationToken cancellationToken)
        {
            IQueryable<Domain.Entities.SerialNumber> query = _context.SerialNumbers
                .Where(s => !s.IsDeleted);

            // ── Filters ──────────────────────────────────────────────────────

            // ProductId — optional, skip if not provided
            if (request.ProductId.HasValue)
                query = query.Where(s => s.ProductId == request.ProductId.Value);

            // Search — partial match on serial number string
            if (!string.IsNullOrWhiteSpace(request.Search))
                query = query.Where(s => s.SerialNo.Contains(request.Search));

            // Status — map enum int to stored string value
            if (request.Status.HasValue)
            {
                var statusString = request.Status.Value switch
                {
                    SerialNumberStatus.Available => "Available",
                    SerialNumberStatus.Sold => "Sold",
                    _ => null
                };

                if (statusString is not null)
                    query = query.Where(s => s.Status == statusString);
            }

            // Date range
            if (request.FromDate.HasValue)
                query = query.Where(s => s.CreatedDate >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(s => s.CreatedDate <= request.ToDate.Value);

            // ── Sort ─────────────────────────────────────────────────────────
            query = query.OrderByDescending(s => s.CreatedDate);

            // ── Project & paginate ───────────────────────────────────────────
            var projected = query.Select(s => new SerialNumberInfo
            {
                Id = s.Id,
                ProductId = s.ProductId,
                SerialNo = s.SerialNo,
                Status = s.Status,
                Price = s.Price,
                CostPrice = s.CostPrice,
                CreatedDate = s.CreatedDate,
            });

            return await projected.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}