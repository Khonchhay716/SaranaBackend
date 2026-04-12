using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Features.Order;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DashboardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        // [RequirePermission("dashboard:read")]
        public async Task<ActionResult<DataListInDashboardResponse>> GetDashboard([FromQuery] DataListInDashboardQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
}
