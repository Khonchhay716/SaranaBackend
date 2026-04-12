// POS.API/Controllers/StaffController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Features.Staff;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StaffController : ControllerBase
    {
        private readonly IMediator _mediator;

        public StaffController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET api/staff
        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] StaffListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        // GET api/staff/lookup
        [HttpGet("lookup")]
        public async Task<IActionResult> Lookup([FromQuery] StaffLookupListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        // GET api/staff/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new StaffQuery { Id = id }, ct);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // POST api/staff
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StaffCreateCommand command, CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return result.Success
                ? CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result)
                : BadRequest(result);
        }

        // PUT api/staff/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] StaffUpdateCommand command, CancellationToken ct)
        {
            command = command with { Id = id };
            var result = await _mediator.Send(command, ct);

            if (!result.Success)
                return result.Data == null ? NotFound(result) : BadRequest(result);

            return Ok(result);
        }

        // DELETE api/staff/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new StaffDeleteCommand(id), ct);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpGet("tree")]
        public async Task<IActionResult> GetStaffTree()
        {
            var result = await _mediator.Send(new StaffTreeQuery());
            return Ok(result);
        }
    }
}