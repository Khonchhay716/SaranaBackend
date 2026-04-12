using MediatR;
using Microsoft.AspNetCore.Mvc;
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

        // GET api/pointsetup
        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var result = await _mediator.Send(new PointSetupQuery(), ct);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // PUT api/pointsetup
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] PointSetupUpdateCommand command, CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}