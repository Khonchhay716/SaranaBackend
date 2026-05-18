// POS.API/Controllers/CustomerController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
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

        [HttpGet]
        [RequirePermission("customer:read")]
        public async Task<IActionResult> GetList([FromQuery] CustomerListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("lookup")]
        // [RequirePermission("customer:lookup")]
        public async Task<IActionResult> Lookup([FromQuery] CustomerLookupListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [RequirePermission("customer:view")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new CustomerQuery { Id = id }, ct);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [RequirePermission("customer:create")]
        public async Task<IActionResult> Create([FromBody] CustomerCreateCommand command, CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return result.Success
                ? CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result)
                : BadRequest(result);
        }

        [HttpPut("{id:int}")]
        [RequirePermission("customer:update")]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerUpdateCommand command, CancellationToken ct)
        {
            command = command with { Id = id };
            var result = await _mediator.Send(command, ct);

            if (!result.Success)
                return result.Data == null ? NotFound(result) : BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [RequirePermission("customer:delete")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new CustomerDeleteCommand(id), ct);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}