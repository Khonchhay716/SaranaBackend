// POS.API/Controllers/BranchController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
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

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] BranchListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> Lookup([FromQuery] BranchLookupListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        // GET api/branch/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new BranchQuery { Id = id }, ct);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // POST api/branch
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BranchCreateCommand command, CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return result.Success
                ? CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result)
                : BadRequest(result);
        }

        // PUT api/branch/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] BranchUpdateCommand command, CancellationToken ct)
        {
            // ✅ Assign Id from route — body no longer needs an "id" field
            command = command with { Id = id };

            var result = await _mediator.Send(command, ct);

            if (!result.Success)
                return result.Data == null ? NotFound(result) : BadRequest(result);

            return Ok(result);
        }

        // DELETE api/branch/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new BranchDeleteCommand(id), ct);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}