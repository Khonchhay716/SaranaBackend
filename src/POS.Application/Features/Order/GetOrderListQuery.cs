// POS.Application/Features/Orders/OrderListQuery.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using POS.Domain.Enums;

namespace POS.Application.Features.Orders
{
    public class OrderListQuery : PaginationRequest, IRequest<PaginatedResult<OrderListInfo>>
    {
        public string? Search { get; set; }
        public int? CustomerId { get; set; }
        public int? StaffId { get; set; }
        public OrderStatus? Status { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
    }

    public class OrderListQueryValidator : AbstractValidator<OrderListQuery>
    {
        public OrderListQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate!.Value)
                .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
                .WithMessage("ToDate must be after FromDate.");
        }
    }

    public class OrderListInfo
    {
        public int Id { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public string? Note { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public int? CreateBy { get; set; }

        // ✅ "NotApplicable" = no serialized lines (nothing to scan out).
        // "Pending" = has serialized lines, none scanned yet.
        // "Partial" = some serialized lines scanned, some not.
        // "Completed" = every serialized line has been scanned/handed out.
        public string StockOutStatus { get; set; } = string.Empty;
    }

    public class OrderListQueryHandler : IRequestHandler<OrderListQuery, PaginatedResult<OrderListInfo>>
    {
        private readonly IMyAppDbContext _context;

        public OrderListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<OrderListInfo>> Handle(
            OrderListQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Orders
                .Where(o => !o.IsDeleted)
                .AsNoTracking();

            if (request.CustomerId.HasValue && request.CustomerId.Value > 0)
                query = query.Where(o => o.CustomerId == request.CustomerId.Value);

            if (request.StaffId.HasValue && request.StaffId.Value > 0)
                query = query.Where(o => o.CreatedBy == request.StaffId.Value);

            if (request.Status.HasValue)
                query = query.Where(o => o.Status == request.Status.Value);

            if (request.PaymentMethod.HasValue)
                query = query.Where(o => o.PaymentMethod == request.PaymentMethod.Value);

            if (request.FromDate.HasValue)
                query = query.Where(o => o.CreatedDate >= request.FromDate.Value.Date);

            if (request.ToDate.HasValue)
                query = query.Where(o => o.CreatedDate < request.ToDate.Value.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(o =>
                    o.OrderNo.Contains(request.Search) ||
                    (o.Customer != null &&
                        (o.Customer.FirstName.Contains(request.Search) ||
                         o.Customer.LastName.Contains(request.Search))));
            }

            query = query.OrderByDescending(o => o.CreatedDate);

            var projected = query.Select(o => new OrderListInfo
            {
                Id = o.Id,
                OrderNo = o.OrderNo,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer != null ? $"{o.Customer.FirstName} {o.Customer.LastName}" : "Walk-in Customer",
                Status = o.Status.ToString(),
                PaymentMethod = o.PaymentMethod.ToString(),
                SubTotal = o.SubTotal,
                DiscountAmount = o.DiscountAmount,
                TotalAmount = o.TotalAmount,
                ItemCount = o.Items.Count,
                Note = o.Note,
                CreatedDate = o.CreatedDate,
                CreateBy = o.CreatedBy,
                StockOutStatus =
                    !o.Items.Any(i => i.Product.ProductType == ProductType.Serialized)
                        ? "NotApplicable"
                        : o.Items.Where(i => i.Product.ProductType == ProductType.Serialized).All(i => i.SerialNumbers != null)
                            ? "Completed"
                            : o.Items.Where(i => i.Product.ProductType == ProductType.Serialized).Any(i => i.SerialNumbers != null)
                                ? "Partial"
                                : "Pending",
            });

            return await projected.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}