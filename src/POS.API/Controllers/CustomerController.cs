// POS.API/Controllers/CustomerController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Features.Customer;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CustomerController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET api/customer
        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] CustomerListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        // GET api/customer/lookup
        [HttpGet("lookup")]
        public async Task<IActionResult> Lookup([FromQuery] CustomerLookupListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        // GET api/customer/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new CustomerQuery { Id = id }, ct);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // POST api/customer
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerCreateCommand command, CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return result.Success
                ? CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result)
                : BadRequest(result);
        }

        // PUT api/customer/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerUpdateCommand command, CancellationToken ct)
        {
            command = command with { Id = id };
            var result = await _mediator.Send(command, ct);

            if (!result.Success)
                return result.Data == null ? NotFound(result) : BadRequest(result);

            return Ok(result);
        }

        // DELETE api/customer/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new CustomerDeleteCommand(id), ct);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}