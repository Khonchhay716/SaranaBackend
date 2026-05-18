using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.Application.Features.Leave;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveRequestController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LeaveRequestController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("my")]
        [RequirePermission("leave_request:read_my")]
        public async Task<IActionResult> GetMyLeaves(
            [FromQuery] MyLeaveRequestListQuery query, CancellationToken ct)
            => Ok(await _mediator.Send(query, ct));

        [HttpGet]
        [RequirePermission("leave_request:read_all")]
        public async Task<IActionResult> GetList(
            [FromQuery] LeaveRequestListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [RequirePermission("leave_request:view")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new LeaveRequestQuery { Id = id }, ct);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [RequirePermission("leave_request:create")]
        public async Task<IActionResult> Submit(
            [FromBody] LeaveRequestSubmitCommand command, CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return result.Success
                ? CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result)
                : BadRequest(result);
        }

        [HttpPut("{id:int}/approve")]
        [RequirePermission("leave_request:approve")]
        public async Task<IActionResult> Approve(
            int id, [FromBody] LeaveRequestApproveCommand command, CancellationToken ct)
        {
            command = command with { Id = id };
            var result = await _mediator.Send(command, ct);
            if (!result.Success)
                return result.Data == null ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{id:int}/reject")]
        [RequirePermission("leave_request:reject")]
        public async Task<IActionResult> Reject(
            int id, [FromBody] LeaveRequestRejectCommand command, CancellationToken ct)
        {
            command = command with { Id = id };
            var result = await _mediator.Send(command, ct);
            if (!result.Success)
                return result.Data == null ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [RequirePermission("leave_request:cancel")]
        public async Task<IActionResult> Cancel(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new LeaveRequestCancelCommand(id), ct);
            return result.Success ? Ok(result) : NotFound(result);
        }


        [HttpPost("calculate")]
        public async Task<IActionResult> Calculate(
            [FromBody] WorkingDayCalculateCommand command,
            CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}