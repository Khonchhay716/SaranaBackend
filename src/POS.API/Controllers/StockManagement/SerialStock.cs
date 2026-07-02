using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Extensions;
using POS.Application.Features.StockManagement;

namespace POS.API.Controllers.StockManagement
{
    [ApiController]
    [Route("api/stock")]

    public class SerialStockController : ControllerBase
    {
        private readonly IMediator _mediator;
        public SerialStockController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]

        public async Task<IActionResult> GetList([FromQuery] SerialStockListQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }


        [HttpGet("available")]
        public async Task<IActionResult> GetAvailable([FromQuery] AvailableSerialNumberQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }

}