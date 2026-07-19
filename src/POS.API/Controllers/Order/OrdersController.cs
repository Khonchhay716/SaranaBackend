using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.API.Extensions;
using POS.Application.Features.Orders;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrdersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Preview order calculation (subtotal, discount, total, warnings) — does NOT save to database.
        /// </summary>
        [HttpPost("summary")]
        public async Task<IActionResult> GetSummary([FromBody] OrderSummaryQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Create order — deducts stock, applies discount, handles point earn/redeem, saves to database.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] OrderListQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(int id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new OrderDetailQuery { Id = id }, cancellationToken);
            return this.ToActionResult(result);
        }

        [HttpGet("sales-summary")]
        public async Task<IActionResult> GetSalesSummary([FromQuery] OrderSalesSummaryQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return this.ToActionResult(result);
        }

        /// <summary>
        /// Serialized lines of an order that are paid but not yet handed out (no serial assigned yet).
        /// Staff looks this up by Order No before scanning serials at stock-out.
        /// </summary>
        [HttpGet("pending-items")]
        public async Task<IActionResult> GetPendingItems([FromQuery] OrderPendingItemsQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return this.ToActionResult(result);
        }
    }
}