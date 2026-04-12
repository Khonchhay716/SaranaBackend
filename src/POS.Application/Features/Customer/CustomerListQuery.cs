using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;

namespace POS.Application.Features.Customer
{
    public class CustomerListQuery : PaginationRequest, IRequest<PaginatedResult<CustomerInfo>>
    {
        public string? Search { get; set; }
        public bool? Status { get; set; }
    }

    public class CustomerListQueryValidator : AbstractValidator<CustomerListQuery>
    {
        public CustomerListQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    public class CustomerListQueryHandler : IRequestHandler<CustomerListQuery, PaginatedResult<CustomerInfo>>
    {
        private readonly IMyAppDbContext _context;

        public CustomerListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<CustomerInfo>> Handle(
            CustomerListQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Customers
                .Where(c => !c.IsDeleted)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(c =>
                    c.FirstName.Contains(request.Search) ||
                    c.LastName.Contains(request.Search) ||
                    c.PhoneNumber.Contains(request.Search));
            }

            if (request.Status.HasValue)
                query = query.Where(c => c.Status == request.Status.Value);

            query = query.OrderByDescending(c => c.CreatedDate);

            var projected = query.Select(c => new CustomerInfo
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                ImageProfile = c.ImageProfile,
                PhoneNumber = c.PhoneNumber,
                TotalPoint = c.TotalPoint,
                Status = c.Status,
                IsDeleted = c.IsDeleted,
                CreatedDate = c.CreatedDate,
                CreatedBy = c.CreatedBy,
                UpdatedDate = c.UpdatedDate,
                UpdatedBy = c.UpdatedBy,
                DeletedDate = c.DeletedDate,
                DeletedBy = c.DeletedBy,
                User = c.Person != null && !c.Person.IsDeleted
                    ? new LinkedUserInfo
                    {
                        Id = c.Person.Id,
                        Username = c.Person.Username,
                        Email = c.Person.Email,
                        IsActive = c.Person.IsActive
                    }
                    : null
            });

            return await projected.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}