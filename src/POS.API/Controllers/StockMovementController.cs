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
        [RequirePermission("manage_stock:all")]
        public async Task<IActionResult> GetList([FromQuery] StockMovementListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
 
        [HttpPost]
        [RequirePermission("manage_stock:all")]
        public async Task<IActionResult> Create([FromBody] StockMovementCreateCommand command, CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return result.Success ? Ok(result) : BadRequest(result);
        }
 
        [HttpPut("{id:int}")]
        [RequirePermission("manage_stock:all")]
        public async Task<IActionResult> Update(int id, [FromBody] StockMovementUpdateCommand command, CancellationToken ct)
        {
            command = command with { Id = id };
            var result = await _mediator.Send(command, ct);
            if (!result.Success)
                return result.Data == null ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }
 
        [HttpDelete("{id:int}")]
        [RequirePermission("manage_stock:all")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new StockMovementDeleteCommand(id), ct);
            if (!result.Success)
                return result.Data == false ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }
    }
}