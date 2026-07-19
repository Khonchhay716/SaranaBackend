using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.API.Extensions;
using POS.Application.Common.Dto;
using POS.Application.Features.StockManagement.StockMovements;

namespace POS.API.Controllers.StockManagement
{
    [ApiController]
    [Route("api/stock")]
    public class StockMovementController : ControllerBase
    {
        private readonly IMediator _mediator;
        public StockMovementController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("movements")]
        [RequirePermission("stockmovement:read")]
        public async Task<ActionResult<PaginatedResult<StockMovementInfo>>> GetList(
            [FromQuery] StockMovementListQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("stockmovement:view")]
        public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetStockMovementByIdQuery { Id = id }, cancellationToken);
            return this.ToActionResult(result);
        }

        [HttpPost("in")]
        [RequirePermission("stockmovement:create")]
        public async Task<IActionResult> StockIn(
            [FromBody] StockInCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return this.ToActionResult(result);
        }

        [HttpPost("out")]
        [RequirePermission("stockmovement:create")]
        public async Task<IActionResult> StockOut(
            [FromBody] StockOutCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return this.ToActionResult(result);
        }

        [HttpGet("out")]
        [RequirePermission("stockmovement:read")]
        public async Task<ActionResult<PaginatedResult<StockMovementInfo>>> GetStockOutList(
            [FromQuery] StockOutListQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }


        [HttpGet("current")]
        public async Task<ActionResult<PaginatedResult<CurrentStockInfo>>> GetCurrentStock([FromQuery] CurrentStockListQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("in/summary")]
        public async Task<ActionResult<StockInSummaryResult>> GetStockInSummary(
        [FromQuery] StockInSummaryQuery query,
        CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}