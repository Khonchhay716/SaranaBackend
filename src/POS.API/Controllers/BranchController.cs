// POS.API/Controllers/BranchController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.Application.Features.Branch;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BranchController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BranchController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("lookup")]
        // [RequirePermission("branch:lookup")]
        public async Task<IActionResult> Lookup([FromQuery] BranchLookupListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet]
        [RequirePermission("branch:read")]
        public async Task<IActionResult> GetList([FromQuery] BranchListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [RequirePermission("branch:view")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new BranchQuery { Id = id }, ct);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [RequirePermission("branch:create")]
        public async Task<IActionResult> Create([FromBody] BranchCreateCommand command, CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return result.Success
                ? CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result)
                : BadRequest(result);
        }

        [HttpPut("{id:int}")]
        [RequirePermission("branch:update")]
        public async Task<IActionResult> Update(int id, [FromBody] BranchUpdateCommand command, CancellationToken ct)
        {
            command = command with { Id = id };

            var result = await _mediator.Send(command, ct);

            if (!result.Success)
                return result.Data == null ? NotFound(result) : BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [RequirePermission("branch:delete")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new BranchDeleteCommand(id), ct);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}