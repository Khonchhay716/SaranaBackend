using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.API.Extensions;
using POS.Application.Common.Dto;
using POS.Application.Features.Order;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrderController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        // [RequirePermission("order:read")]
        public async Task<ActionResult<PaginatedResult<OrderInfo>>> GetOrders([FromQuery] OrderListQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        // [RequirePermission("order:read")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var query = new OrderQuery { Id = id };
            var result = await _mediator.Send(query);
            return this.ToActionResult(result);
        }

        [HttpPost]
        // [RequirePermission("order:create")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateCommand command)
        {
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }
    }
}