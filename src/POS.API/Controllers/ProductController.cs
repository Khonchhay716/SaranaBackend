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
    public class ProductController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProductController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("Sale-POS")]
        public async Task<IActionResult> SaleLookup(
            [FromQuery] ProductSaleLookupQuery query,
            CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }


        [HttpGet]
        [RequirePermission("product:read")]
        public async Task<ActionResult<PaginatedResult<ProductInfo>>> GetProducts([FromQuery] ProductListQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("product:create")]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateCommand command)
        {
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("product:read")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var query = new ProductQuery { Id = id };
            var result = await _mediator.Send(query);
            return this.ToActionResult(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("product:update")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpdateCommand command)
        {
            command.Id = id;
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("product:delete")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var command = new ProductDeleteCommand { Id = id };
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }


        [HttpGet("{id:int}/summary")]
        [RequirePermission("product:read")]
        public async Task<IActionResult> GetStockSummary(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new ProductStockSummaryQuery { Id = id }, ct);
            return this.ToActionResult(result);
        }

        [HttpGet("stock-summary")]
        [RequirePermission("product:read")]
        public async Task<IActionResult> GetStockSummaryList(
        [FromQuery] ProductStockTotalSummaryQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

    }
}