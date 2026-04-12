using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.PointSetup
{
    public class PointSetupQuery : IRequest<ApiResponse<PointSetupInfo>> { }

    public class PointSetupQueryHandler : IRequestHandler<PointSetupQuery, ApiResponse<PointSetupInfo>>
    {
        private readonly IMyAppDbContext _context;

        public PointSetupQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PointSetupInfo>> Handle(
            PointSetupQuery   request,
            CancellationToken cancellationToken)
        {
            var config = await _context.PointSetups
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == 1, cancellationToken);

            if (config == null)
                return ApiResponse<PointSetupInfo>.NotFound("Point setup configuration not found.");

            return ApiResponse<PointSetupInfo>.Ok(config.Adapt<PointSetupInfo>(), "Point setup retrieved successfully");
        }
    }
}