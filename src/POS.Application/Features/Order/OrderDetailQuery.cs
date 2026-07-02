// POS.Application/Features/Orders/OrderDetailQuery.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;

namespace POS.Application.Features.Orders
{
    public record OrderDetailQuery : IRequest<ApiResponse<OrderInfo>>
    {
        public int Id { get; set; }
    }

    public class OrderDetailQueryValidator : AbstractValidator<OrderDetailQuery>
    {
        public OrderDetailQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Order Id is required.");
        }
    }

    public class OrderDetailQueryHandler : IRequestHandler<OrderDetailQuery, ApiResponse<OrderInfo>>
    {
        private readonly IMyAppDbContext _context;
        public OrderDetailQueryHandler(IMyAppDbContext context) => _context = context;

        public async Task<ApiResponse<OrderInfo>> Handle(OrderDetailQuery request, CancellationToken cancellationToken)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Items).ThenInclude(i => i.Discount)
                .FirstOrDefaultAsync(o => o.Id == request.Id && !o.IsDeleted, cancellationToken);

            if (order == null)
                return ApiResponse<OrderInfo>.NotFound("Order not found.");
            TypeNamebase? createBy = null;
            if (order.CreatedBy.HasValue)
            {
                createBy = await _context.Persons
                    .Where(u => u.Id == order.CreatedBy.Value)
                    .Select(u => new TypeNamebase { Id = u.Id, Name = u.Username })
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var info = CreateOrderCommandHandler.MapToInfo(order);
            info.CreateBy = createBy;

            return ApiResponse<OrderInfo>.Ok(info, "Order detail retrieved successfully.");
        }
    }
}