// POS.API/Controllers/DiscountController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.Application.Features.Discount;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiscountController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DiscountController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [RequirePermission("discount:read")]
        public async Task<IActionResult> GetList([FromQuery] DiscountListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("lookup")]
        // [RequirePermission("discount:lookup")]
        public async Task<IActionResult> Lookup([FromQuery] DiscountLookupListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [RequirePermission("discount:view")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new DiscountQuery { Id = id }, ct);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [RequirePermission("discount:create")]
        public async Task<IActionResult> Create([FromBody] DiscountCreateCommand command, CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return result.Success
                ? CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result)
                : BadRequest(result);
        }

        [HttpPut("{id:int}")]
        [RequirePermission("discount:update")]
        public async Task<IActionResult> Update(int id, [FromBody] DiscountUpdateCommand command, CancellationToken ct)
        {
            command = command with { Id = id };
            var result = await _mediator.Send(command, ct);
            if (!result.Success)
                return result.Data == null ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [RequirePermission("discount:delete")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new DiscountDeleteCommand(id), ct);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}