using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Application.Features.Discount;
using static POS.Application.Features.Discount.DiscountCreateCommandHandler;
public class DiscountLookupListQuery : PaginationRequest, IRequest<PaginatedResult<DiscountInfoLookup>>
{
    public string? Search { get; set; }
}

public class DiscountLookupListQueryHandler : IRequestHandler<DiscountLookupListQuery, PaginatedResult<DiscountInfoLookup>>
{
    private readonly IMyAppDbContext _context;

    public DiscountLookupListQueryHandler(IMyAppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<DiscountInfoLookup>> Handle(DiscountLookupListQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Discounts
            .Where(d => !d.IsDeleted && d.IsActive)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(d => d.Name.Contains(request.Search));

        query = query.OrderByDescending(d => d.CreatedDate);

        var projected = query.Select(d => new DiscountInfoLookup
        {
            Id = d.Id,
            Name = d.Name,
            Type = d.Type,
            Value = d.Value,
        });

        return await projected.ToPaginatedResultAsync(request.Page, request.PageSize);
    }
}
