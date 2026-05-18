using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.Application.Features.PointSetup;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PointSetupController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PointSetupController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpGet("lookup")]
        public async Task<IActionResult> GetLookUp(CancellationToken ct)
        {
            var result = await _mediator.Send(new PointSetupQuery(), ct);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpGet]
        [RequirePermission("point_setting:view")]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var result = await _mediator.Send(new PointSetupQuery(), ct);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPut]
        [RequirePermission("point_setting:update")]
        public async Task<IActionResult> Update([FromBody] PointSetupUpdateCommand command, CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}