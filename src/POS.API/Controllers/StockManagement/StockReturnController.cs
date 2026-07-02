using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.API.Extensions;
using POS.Application.Common.Dto;
using POS.Application.Features.StockManagement.StockReturns;

namespace POS.API.Controllers.StockManagement
{
    [ApiController]
    [Route("api/stock/returns")]
    public class StockReturnController : ControllerBase
    {
        private readonly IMediator _mediator;
        
        public StockReturnController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [RequirePermission("stockreturn:read")]
        public async Task<ActionResult<PaginatedResult<StockReturnInfo>>> GetList(
            [FromQuery] StockReturnListQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("stockreturn:view")]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetStockReturnByIdQuery { Id = id }, cancellationToken);
            return this.ToActionResult(result);
        }

        [HttpPost]
        [RequirePermission("stockreturn:create")]
        public async Task<IActionResult> Create(
            [FromBody] CreateStockReturnCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return this.ToActionResult(result);
        }

        [HttpPost("{id}/cancel")]
        [RequirePermission("stockreturn:cancel")]
        public async Task<IActionResult> Cancel(
            int id,
            [FromBody] CancelStockReturnCommand command, 
            CancellationToken cancellationToken)
        {
            var finalCommand = command with { Id = id };
            
            var result = await _mediator.Send(finalCommand, cancellationToken);
            return this.ToActionResult(result);
        }
    }
}