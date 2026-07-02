// POS.Application/Features/StockManagement/AvailableSerialNumberQuery.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;

namespace POS.Application.Features.StockManagement
{
    public class AvailableSerialNumberQuery : PaginationRequest, IRequest<PaginatedResult<SerialStockInfo>>
    {
        public int ProductId { get; set; }
        public string? Search { get; set; }
    }

    public class AvailableSerialNumberQueryValidator : AbstractValidator<AvailableSerialNumberQuery>
    {
        public AvailableSerialNumberQueryValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("ProductId is required.");
            RuleFor(x => x.Page).GreaterThan(0).WithMessage("Page must be greater than 0.");
            RuleFor(x => x.PageSize).GreaterThan(0).WithMessage("PageSize must be greater than 0.");
        }
    }

    public class AvailableSerialNumberQueryHandler : IRequestHandler<AvailableSerialNumberQuery, PaginatedResult<SerialStockInfo>>
    {
        private readonly IMyAppDbContext _context;
        public AvailableSerialNumberQueryHandler(IMyAppDbContext context) => _context = context;

        public async Task<PaginatedResult<SerialStockInfo>> Handle(AvailableSerialNumberQuery request, CancellationToken cancellationToken)
        {
            var query = _context.SerialStocks
                .Where(s => !s.IsDeleted
                    && s.ProductId == request.ProductId
                    && s.Status == SerialStatus.Available);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(s => s.SerialNo.Contains(request.Search));
            }

            query = query.OrderBy(s => s.SerialNo);

            var result = query.Select(s => new SerialStockInfo
            {
                Id = s.Id,
                ProductId = s.ProductId,
                ProductName = s.Product.Name,
                SerialNo = s.SerialNo,
                Status = s.Status.ToString(),
                CreatedDate = s.CreatedDate
            });

            return await result.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}