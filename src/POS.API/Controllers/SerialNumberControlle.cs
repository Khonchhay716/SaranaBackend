using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.API.Extensions;
using POS.Application.Features.SerialNumber;  // ✅ Query + Commands all here now

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SerialNumberController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SerialNumberController(IMediator mediator)
        {
            _mediator = mediator;
        }


        [HttpGet]
        [RequirePermission("manage_stock:all")]
        public async Task<IActionResult> GetList([FromQuery] SerialNumberListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("manage_stock:all")]
        public async Task<IActionResult> Create([FromBody] SerialNumberCreateCommand command, CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:int}")]
        [RequirePermission("manage_stock:all")]
        public async Task<IActionResult> Update(int id, [FromBody] SerialNumberUpdateCommand command, CancellationToken ct)
        {
            command = command with { Id = id };
            var result = await _mediator.Send(command, ct);
            if (!result.Success)
                return result.Data == null ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [RequirePermission("manage_stock:all")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new SerialNumberDeleteCommand(id), ct);
            if (!result.Success)
                return result.Data == false ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }
    }
}
