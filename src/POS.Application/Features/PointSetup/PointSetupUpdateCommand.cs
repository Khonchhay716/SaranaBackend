using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.PointSetup
{
    public record PointSetupUpdateCommand : IRequest<ApiResponse<PointSetupInfo>>
    {
        // ==================== Earning ====================
        public decimal PointValue          { get; set; } = 0;
        public decimal MinOrderAmount      { get; set; } = 0;
        public int?    MaxPointPerOrder    { get; set; } = null;

        // ==================== Redemption ====================
        public decimal PointsPerRedemption { get; set; } = 0;

        // ==================== Status ====================
        public bool    IsActive            { get; set; } = false;
    }

    public class PointSetupUpdateCommandValidator : AbstractValidator<PointSetupUpdateCommand>
    {
        public PointSetupUpdateCommandValidator()
        {
            RuleFor(x => x.PointValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Point value must be >= 0.");

            RuleFor(x => x.MinOrderAmount)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Min order amount must be >= 0.");

            RuleFor(x => x.MaxPointPerOrder)
                .GreaterThan(0).When(x => x.MaxPointPerOrder.HasValue)
                .WithMessage("Max point per order must be > 0 if set.");

            RuleFor(x => x.PointsPerRedemption)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Points per redemption must be >= 0.");
        }
    }

    public class PointSetupUpdateCommandHandler : IRequestHandler<PointSetupUpdateCommand, ApiResponse<PointSetupInfo>>
    {
        private readonly IMyAppDbContext _context;

        public PointSetupUpdateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PointSetupInfo>> Handle(
            PointSetupUpdateCommand request,
            CancellationToken       cancellationToken)
        {
            var validator        = new PointSetupUpdateCommandValidator();
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return ApiResponse<PointSetupInfo>.BadRequest(errors);
            }

            var config = await _context.PointSetups
                .FirstOrDefaultAsync(p => p.Id == 1, cancellationToken);

            if (config == null)
                return ApiResponse<PointSetupInfo>.NotFound("Point setup configuration not found.");

            // ==================== Update Fields ====================
            config.PointValue          = request.PointValue;
            config.MinOrderAmount      = request.MinOrderAmount;
            config.MaxPointPerOrder    = request.MaxPointPerOrder;
            config.PointsPerRedemption = request.PointsPerRedemption;
            config.IsActive            = request.IsActive;
            config.UpdatedDate         = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<PointSetupInfo>.Ok(config.Adapt<PointSetupInfo>(), "Point setup updated successfully");
        }
    }
}