// POS.Application/Features/Orders/OrderSalesSummaryQuery.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;

namespace POS.Application.Features.Orders
{
    public record OrderSalesSummaryQuery : IRequest<ApiResponse<OrderSalesSummaryInfo>>
    {
        public int? StaffId { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
    }

    public class OrderSalesSummaryQueryValidator : AbstractValidator<OrderSalesSummaryQuery>
    {
        public OrderSalesSummaryQueryValidator()
        {
            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate!.Value)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("EndDate must be after StartDate.");
        }
    }

    public class OrderSalesSummaryInfo
    {
        public decimal TotalSold { get; set; }
        public int TotalOrders { get; set; }

        // ✅ Breakdown តាម payment method
        public decimal SaleByCashTotal { get; set; }
        public decimal SaleByQRTotal { get; set; }
        public decimal SaleByPointTotal { get; set; }
    }

    public class OrderSalesSummaryQueryHandler : IRequestHandler<OrderSalesSummaryQuery, ApiResponse<OrderSalesSummaryInfo>>
    {
        private readonly IMyAppDbContext _context;
        public OrderSalesSummaryQueryHandler(IMyAppDbContext context) => _context = context;

        public async Task<ApiResponse<OrderSalesSummaryInfo>> Handle(OrderSalesSummaryQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Orders
                .Where(o => !o.IsDeleted && o.Status == OrderStatus.Completed)
                .AsNoTracking();

            if (request.StaffId.HasValue && request.StaffId.Value > 0)
                query = query.Where(o => o.CreatedBy == request.StaffId.Value);

            if (request.StartDate.HasValue)
                query = query.Where(o => o.CreatedDate >= request.StartDate.Value.Date);

            if (request.EndDate.HasValue)
                query = query.Where(o => o.CreatedDate < request.EndDate.Value.Date.AddDays(1));

            var totalSold = await query.SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0;
            var totalOrders = await query.CountAsync(cancellationToken);

            var groupedSales = await query
                .GroupBy(o => o.PaymentMethod)
                .Select(g => new { PaymentMethod = g.Key, Total = g.Sum(o => o.TotalAmount) })
                .ToListAsync(cancellationToken);

            var result = new OrderSalesSummaryInfo
            {
                TotalSold = totalSold,
                TotalOrders = totalOrders,
                SaleByCashTotal = groupedSales.FirstOrDefault(g => g.PaymentMethod == PaymentMethod.Cash)?.Total ?? 0,
                SaleByQRTotal = groupedSales.FirstOrDefault(g => g.PaymentMethod == PaymentMethod.QRCode)?.Total ?? 0,
                SaleByPointTotal = groupedSales.FirstOrDefault(g => g.PaymentMethod == PaymentMethod.Point)?.Total ?? 0,
            };

            return ApiResponse<OrderSalesSummaryInfo>.Ok(result, "Sales summary retrieved successfully.");
        }
    }
}