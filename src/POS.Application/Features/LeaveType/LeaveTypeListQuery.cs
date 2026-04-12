using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;

namespace POS.Application.Features.Leave
{
    // ===== LIST =====
    public class LeaveTypeListQuery : PaginationRequest, IRequest<PaginatedResult<LeaveTypeInfo>>
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
    }

    public class LeaveTypeListQueryValidator : AbstractValidator<LeaveTypeListQuery>
    {
        public LeaveTypeListQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    public class LeaveTypeListQueryHandler : IRequestHandler<LeaveTypeListQuery, PaginatedResult<LeaveTypeInfo>>
    {
        private readonly IMyAppDbContext _context;

        public LeaveTypeListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<LeaveTypeInfo>> Handle(
            LeaveTypeListQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.LeaveTypes
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.Search))
                query = query.Where(x => x.Name.ToLower().Contains(request.Search.ToLower()));

            if (request.IsActive.HasValue)
                query = query.Where(x => x.IsActive == request.IsActive.Value);

            query = query.OrderByDescending(x => x.CreatedDate);

            var projected = query.Select(x => new LeaveTypeInfo
            {
                Id = x.Id,
                Name = x.Name,
                MaxDaysPerYear = x.MaxDaysPerYear,
                Description = x.Description,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedDate = x.CreatedDate,
                CreatedBy = x.CreatedBy,
                UpdatedDate = x.UpdatedDate,
                UpdatedBy = x.UpdatedBy,
                DeletedDate = x.DeletedDate,
                DeletedBy = x.DeletedBy,
            });

            return await projected.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}