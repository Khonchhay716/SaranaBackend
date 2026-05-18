// POS.API/Controllers/StaffController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
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

        [HttpGet]
        [RequirePermission("staff:read")]
        public async Task<IActionResult> GetList([FromQuery] StaffListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("lookup")]
        // [RequirePermission("staff:lookup")]
        public async Task<IActionResult> Lookup([FromQuery] StaffLookupListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [RequirePermission("staff:view")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new StaffQuery { Id = id }, ct);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [RequirePermission("staff:create")]
        public async Task<IActionResult> Create([FromBody] StaffCreateCommand command, CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return result.Success
                ? CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result)
                : BadRequest(result);
        }

        [HttpPut("{id:int}")]
        [RequirePermission("staff:update")]
        public async Task<IActionResult> Update(int id, [FromBody] StaffUpdateCommand command, CancellationToken ct)
        {
            command = command with { Id = id };
            var result = await _mediator.Send(command, ct);

            if (!result.Success)
                return result.Data == null ? NotFound(result) : BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [RequirePermission("staff:delete")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new StaffDeleteCommand(id), ct);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpGet("tree")]
        [RequirePermission("staff:read")]
        public async Task<IActionResult> GetStaffTree()
        {
            var result = await _mediator.Send(new StaffTreeQuery());
            return Ok(result);
        }
    }
}