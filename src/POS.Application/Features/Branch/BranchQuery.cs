// POS.Application/Features/Branch/BranchQuery.cs
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;

namespace POS.Application.Features.Branch
{
    // ─────────────────────────────────────────────────────────
    // Single Branch
    // ─────────────────────────────────────────────────────────
    public class BranchQuery : IRequest<ApiResponse<BranchInfo>>
    {
        public int Id { get; set; }
    }

    public class BranchQueryHandler : IRequestHandler<BranchQuery, ApiResponse<BranchInfo>>
    {
        private readonly IMyAppDbContext _context;

        public BranchQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<BranchInfo>> Handle(
            BranchQuery       request,
            CancellationToken cancellationToken)
        {
            var branch = await _context.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == request.Id && !b.IsDeleted, cancellationToken);

            if (branch == null)
                return ApiResponse<BranchInfo>.NotFound($"Branch with id {request.Id} was not found.");

            return ApiResponse<BranchInfo>.Ok(branch.Adapt<BranchInfo>(), "Branch retrieved successfully");
        }
    }

    // ─────────────────────────────────────────────────────────
    // Paginated List
    // ─────────────────────────────────────────────────────────
    public class BranchListQuery : PaginationRequest, IRequest<PaginatedResult<BranchInfo>>
    {
        public string? Search { get; set; }

        /// <summary>Filter by status: "Active" | "Inactive" | null = all</summary>
        public string? Status { get; set; }
    }

    public class BranchListQueryValidator : AbstractValidator<BranchListQuery>
    {
        public BranchListQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    public class BranchListQueryHandler : IRequestHandler<BranchListQuery, PaginatedResult<BranchInfo>>
    {
        private readonly IMyAppDbContext _context;

        public BranchListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<BranchInfo>> Handle(
            BranchListQuery   request,
            CancellationToken cancellationToken)
        {
            var query = _context.Branches
                .Where(b => !b.IsDeleted)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(b =>
                    b.BranchName.Contains(request.Search) ||
                    (b.Description != null && b.Description.Contains(request.Search)));
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
                query = query.Where(b => b.Status == request.Status);

            query = query.OrderByDescending(b => b.CreatedDate);

            var projected = query.Select(b => new BranchInfo
            {
                Id          = b.Id,
                BranchName  = b.BranchName,
                Logo        = b.Logo,
                Status      = b.Status,
                Description = b.Description,
                IsDeleted   = b.IsDeleted,
                CreatedDate = b.CreatedDate,
                CreatedBy   = b.CreatedBy,
                UpdatedDate = b.UpdatedDate,
                UpdatedBy   = b.UpdatedBy,
                DeletedDate = b.DeletedDate,
                DeletedBy   = b.DeletedBy,
            });

            return await projected.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}