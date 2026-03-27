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
 
        // GET api/serial-number?productId=5&page=1&pageSize=20&status=Available&search=IMEI
        [HttpGet]
        [RequirePermission("product:read")]
        public async Task<IActionResult> GetList([FromQuery] SerialNumberListQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
 
        // POST api/serial-number
        [HttpPost]
        [RequirePermission("product:update")]
        public async Task<IActionResult> Create([FromBody] SerialNumberCreateCommand command, CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return result.Success ? Ok(result) : BadRequest(result);
        }
 
        // PUT api/serial-number/5
        [HttpPut("{id:int}")]
        [RequirePermission("product:update")]
        public async Task<IActionResult> Update(int id, [FromBody] SerialNumberUpdateCommand command, CancellationToken ct)
        {
            command = command with { Id = id };
            var result = await _mediator.Send(command, ct);
            if (!result.Success)
                return result.Data == null ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }
 
        // DELETE api/serial-number/5
        [HttpDelete("{id:int}")]
        [RequirePermission("product:update")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new SerialNumberDeleteCommand(id), ct);
            if (!result.Success)
                return result.Data == false ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }
    }
}
 