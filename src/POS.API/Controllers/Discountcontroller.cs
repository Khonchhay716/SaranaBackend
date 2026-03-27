// POS.API/Controllers/DiscountController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
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

        // GET api/discount?page=1&pageSize=10&search=sale&isActive=true&isGlobal=true
        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] DiscountListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        // GET api/discount/lookup?page=1&pageSize=20&search=sale
        [HttpGet("lookup")]
        public async Task<IActionResult> Lookup([FromQuery] DiscountLookupListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        // GET api/discount/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new DiscountQuery { Id = id }, ct);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // POST api/discount
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DiscountCreateCommand command, CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return result.Success
                ? CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result)
                : BadRequest(result);
        }

        // PUT api/discount/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] DiscountUpdateCommand command, CancellationToken ct)
        {
            command = command with { Id = id };
            var result = await _mediator.Send(command, ct);
            if (!result.Success)
                return result.Data == null ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }

        // DELETE api/discount/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new DiscountDeleteCommand(id), ct);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}