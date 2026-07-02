using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.API.Extensions;
using POS.Application.Common.Dto;
using POS.Application.Features.StockManagement.StockAdjustments;

namespace POS.API.Controllers.StockManagement
{
    [ApiController]
    [Route("api/stock")]
    public class StockAdjustmentController : ControllerBase
    {
        private readonly IMediator _mediator;
        public StockAdjustmentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("adjustments")]
        [RequirePermission("adjustment:read")]
        public async Task<ActionResult<PaginatedResult<StockAdjustmentInfo>>> GetList(
            [FromQuery] StockAdjustmentListQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpPost("adjustments")]
        [RequirePermission("adjustment:create")]
        public async Task<IActionResult> Adjust(
            [FromBody] StockAdjustCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return this.ToActionResult(result);
        }

        [HttpGet("adjustments/{id}")]
        [RequirePermission("adjustment:view")]
        public async Task<IActionResult> GetById(
                    int id,
                    CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetStockAdjustmentByIdQuery { Id = id }, cancellationToken);
            return this.ToActionResult(result);
        }
    }
}