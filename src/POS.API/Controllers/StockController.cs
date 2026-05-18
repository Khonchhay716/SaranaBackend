using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.API.Extensions;
using POS.Application.Common.Dto;
using POS.Application.Features.Product;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockController : ControllerBase
    {
        private readonly IMediator _mediator;

        public StockController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [RequirePermission("manage_stock:all")]
        public async Task<ActionResult<PaginatedResult<ProductInfo>>> GetProducts([FromQuery] ProductListQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }


        [HttpGet("{id:int}/summary")]
        // [RequirePermission("manage_stock:all")]
        public async Task<IActionResult> GetStockSummary(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new ProductStockSummaryQuery { Id = id }, ct);
            return this.ToActionResult(result);
        }

        [HttpGet("stock-summary")]
        // [RequirePermission("manage_stock:all")]
        public async Task<IActionResult> GetStockSummaryList(
        [FromQuery] ProductStockTotalSummaryQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
    }
}