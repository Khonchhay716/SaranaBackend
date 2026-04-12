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
    public enum TypeProduct
    {
        Serial   = 1,
        NoSerial = 2,
    }

    public class OrderListQuery : PaginationRequest, IRequest<PaginatedResult<OrderListResponse>>
    {
        public string?         Search        { get; set; }
        public int?            CustomerId    { get; set; }
        public int?            StaffId       { get; set; }
        public OrderStatus?    Status        { get; set; }
        public PaymentStatus?  PaymentStatus { get; set; }
        public DateTimeOffset? FromDate      { get; set; }
        public DateTimeOffset? ToDate        { get; set; }
        public TypeProduct?    TypeProduct   { get; set; }
    }

    public class OrderListQueryValidator : AbstractValidator<OrderListQuery>
    {
        public OrderListQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
            RuleFor(x => x.TypeProduct)
                .IsInEnum()
                .When(x => x.TypeProduct.HasValue)
                .WithMessage("TypeProduct must be 1 (Serial) or 2 (NoSerial).");
        }
    }

    public class OrderListQueryHandler : IRequestHandler<OrderListQuery, PaginatedResult<OrderListResponse>>
    {
        private readonly IMyAppDbContext _context;

        public OrderListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<OrderListResponse>> Handle(
            OrderListQuery    request,
            CancellationToken cancellationToken)
        {
            var query = _context.Orders
                .Where(o => !o.IsDeleted)
                .AsNoTracking();

            // ==================== Filters ====================
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim().ToLower();
                query = query.Where(o =>
                    o.OrderItems.Any(oi =>
                        oi.SerialNumber != null &&
                        oi.SerialNumber.SerialNo.ToLower().Contains(s)));
            }

            if (request.CustomerId.HasValue)
                query = query.Where(o => o.CustomerId == request.CustomerId.Value);

            if (request.StaffId.HasValue)
                query = query.Where(o => o.StaffId == request.StaffId.Value);

            if (request.Status.HasValue)
                query = query.Where(o => o.Status == request.Status.Value);

            if (request.PaymentStatus.HasValue)
                query = query.Where(o => o.PaymentStatus == request.PaymentStatus.Value);

            if (request.FromDate.HasValue)
                query = query.Where(o => o.OrderDate >= request.FromDate.Value.Date);

            if (request.ToDate.HasValue)
                query = query.Where(o => o.OrderDate <= request.ToDate.Value.Date.AddDays(1).AddTicks(-1));

            if (request.TypeProduct.HasValue)
            {
                if (request.TypeProduct.Value == TypeProduct.Serial)
                    query = query.Where(o => o.OrderItems.Any(oi => !oi.IsDeleted && oi.SerialNumberId != null));
                else
                    query = query.Where(o => o.OrderItems.All(oi => oi.IsDeleted || oi.SerialNumberId == null));
            }

            query = query.OrderByDescending(o => o.OrderDate);

            // ==================== Projection ====================
            var projectedQuery = query.Select(o => new OrderListResponse
            {
                Id             = o.Id,
                OrderNumber    = o.OrderNumber,
                OrderDate      = o.OrderDate,
                CustomerId     = o.CustomerId,
                Staff          = o.Staff != null ? new TypeNamebase
                {
                    Id   = o.Staff.Id,
                    Name = o.Staff.Username
                } : null,
                SubTotal       = o.SubTotal,
                DiscountAmount = o.DiscountAmount,
                TaxAmount      = o.TaxAmount,
                TotalAmount    = o.TotalAmount,

                // ==================== Point ====================
                EarnedPoints   = o.EarnedPoints,  // ✅
                PointsUsed     = o.PointsUsed,    // ✅
                CashReceived   = o.CashReceived,  // ✅

                PaymentMethod = new TypeNamebase
                {
                    Id   = (int)(o.PaymentMethod ?? 0),
                    Name = o.PaymentMethod.HasValue ? o.PaymentMethod.ToString() : "N/A"
                },
                PaymentStatus = new TypeNamebase
                {
                    Id   = (int)o.PaymentStatus,
                    Name = o.PaymentStatus.ToString()
                },
                SaleType = new TypeNamebase
                {
                    Id   = (int)o.SaleType,
                    Name = o.SaleType.ToString()
                },
                Status = new TypeNamebase
                {
                    Id   = (int)o.Status,
                    Name = o.Status.ToString()
                },
                Notes      = o.Notes,
                OrderItems = o.OrderItems
                    .Where(oi => !oi.IsDeleted)
                    .Select(oi => new OrderItemInfo
                    {
                        Id                = oi.Id,
                        OrderId           = oi.OrderId,
                        ProductId         = oi.ProductId,
                        ProductName       = oi.Product.Name,
                        ImageProduct      = oi.ImageProduct ?? "",
                        SerialNumberId    = oi.SerialNumberId,
                        SerialNo          = oi.SerialNumber != null ? new TypeNamebase
                        {
                            Id   = oi.SerialNumber.Id,
                            Name = oi.SerialNumber.SerialNo ?? "N/A"
                        } : null,
                        Quantity          = oi.Quantity,
                        UnitPrice         = oi.UnitPrice,
                        SubTotal          = oi.SubTotal,
                        WarrantyMonths    = oi.WarrantyMonths,
                        WarrantyStartDate = oi.WarrantyStartDate,
                        WarrantyEndDate   = oi.WarrantyEndDate,
                    }).ToList()
            });

            return await projectedQuery.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}