using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.API.Extensions;
using POS.Application.Features.StockMovement;
 
namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockMovementController : ControllerBase
    {
        private readonly IMediator _mediator;
 
        public StockMovementController(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        [HttpGet]
        [RequirePermission("product:read")]
        public async Task<IActionResult> GetList([FromQuery] StockMovementListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
 
        // POST api/stock-movement
        [HttpPost]
        [RequirePermission("product:update")]
        public async Task<IActionResult> Create([FromBody] StockMovementCreateCommand command, CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return result.Success ? Ok(result) : BadRequest(result);
        }
 
        // PUT api/stock-movement/5
        [HttpPut("{id:int}")]
        [RequirePermission("product:update")]
        public async Task<IActionResult> Update(int id, [FromBody] StockMovementUpdateCommand command, CancellationToken ct)
        {
            command = command with { Id = id };
            var result = await _mediator.Send(command, ct);
            if (!result.Success)
                return result.Data == null ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }
 
        // DELETE api/stock-movement/5
        [HttpDelete("{id:int}")]
        [RequirePermission("product:update")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new StockMovementDeleteCommand(id), ct);
            if (!result.Success)
                return result.Data == false ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }
    }
}