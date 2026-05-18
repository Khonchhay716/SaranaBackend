using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.Application.Features.Leave;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveTypeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LeaveTypeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("lookup")]
        // [RequirePermission("leave_type:lookup")]
        public async Task<IActionResult> Lookup(
            [FromQuery] LeaveTypeLookupListQuery query, CancellationToken ct)
            => Ok(await _mediator.Send(query, ct));

        [HttpGet]
        [RequirePermission("leave_type:read")]
        public async Task<IActionResult> GetList(
            [FromQuery] LeaveTypeListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [RequirePermission("leave_type:view")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new LeaveTypeQuery { Id = id }, ct);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [RequirePermission("leave_type:create")]
        public async Task<IActionResult> Create(
            [FromBody] LeaveTypeCreateCommand command, CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return result.Success
                ? CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result)
                : BadRequest(result);
        }

        [HttpPut("{id:int}")]
        [RequirePermission("leave_type:update")]
        public async Task<IActionResult> Update(
            int id, [FromBody] LeaveTypeUpdateCommand command, CancellationToken ct)
        {
            command = command with { Id = id };
            var result = await _mediator.Send(command, ct);
            if (!result.Success)
                return result.Data == null ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [RequirePermission("leave_type:delete")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new LeaveTypeDeleteCommand(id), ct);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}