using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using POS.Domain.Enums;

namespace POS.Application.Features.Order
{
    public class OrderListQuery : PaginationRequest, IRequest<PaginatedResult<OrderInfo>>
    {
        public string? Search { get; set; }
        public int? CustomerId { get; set; }
        public int? StaffId { get; set; }
        public OrderStatus? Status { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
    }

    public class OrderListQueryValidator : AbstractValidator<OrderListQuery>
    {
        public OrderListQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    public class OrderListQueryHandler : IRequestHandler<OrderListQuery, PaginatedResult<OrderInfo>>
    {
        private readonly IMyAppDbContext _context;

        public OrderListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<OrderInfo>> Handle(OrderListQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Orders
                // .Include(o => o.Customer)
                // .Include(o => o.Staff)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SerialNumber)
                .Where(o => !o.IsDeleted)
                .AsNoTracking();

            // Filters
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(o => o.OrderNumber.Contains(request.Search));
            }

            if (request.CustomerId.HasValue)
            {
                query = query.Where(o => o.CustomerId == request.CustomerId.Value);
            }

            if (request.StaffId.HasValue)
            {
                query = query.Where(o => o.StaffId == request.StaffId.Value);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(o => o.Status == request.Status.Value);
            }

            if (request.PaymentStatus.HasValue)
            {
                query = query.Where(o => o.PaymentStatus == request.PaymentStatus.Value);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(o => o.OrderDate <= request.ToDate.Value);
            }

            query = query.OrderByDescending(o => o.OrderDate);

            var projectedQuery = query.Select(o => new OrderInfo
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                CustomerId = o.CustomerId,
                // Customer = o.Customer != null ? new TypeNamebase { Id = o.Customer.Id, Name = o.Customer.Name } : null,
                StaffId = o.StaffId,
                // Staff = o.Staff != null ? new TypeNamebase { Id = o.Staff.Id, Name = o.Staff.Username } : null,
                SubTotal = o.SubTotal,
                DiscountAmount = o.DiscountAmount,
                TaxAmount = o.TaxAmount,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                StatusName = o.Status.ToString(),
                SaleType = o.SaleType,
                SaleTypeName = o.SaleType.ToString(),
                PaymentStatus = o.PaymentStatus,
                PaymentStatusName = o.PaymentStatus.ToString(),
                PaymentMethod = o.PaymentMethod,
                PaymentMethodName = o.PaymentMethod.ToString(),
                Notes = o.Notes,
                CreatedDate = o.CreatedDate,
                CreatedBy = o.CreatedBy,
                OrderItems = o.OrderItems
                    .Where(oi => !oi.IsDeleted)
                    .Select(oi => new OrderItemInfo
                    {
                        Id = oi.Id,
                        OrderId = oi.OrderId,
                        ProductId = oi.ProductId,
                        ProductName = oi.Product.Name,
                        ProductSKU = oi.Product.SKU,
                        SerialNumberId = oi.SerialNumberId,
                        SerialNo = oi.SerialNumber != null ? oi.SerialNumber.SerialNo : null,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        SubTotal = oi.SubTotal,
                        Notes = oi.Notes
                    }).ToList()
            });

            return await projectedQuery.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}