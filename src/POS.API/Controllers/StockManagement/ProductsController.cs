using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.API.Extensions;
using POS.Application.Common.Dto;
using POS.Application.Features.StockManagement.Products;

namespace POS.API.Controllers.StockManagement
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ProductsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [RequirePermission("product:read")]
        public async Task<ActionResult<PaginatedResult<ProductInfo>>> GetList(
            [FromQuery] ProductListQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        [HttpGet("low-stock")]
        [RequirePermission("product:read")]
        public async Task<IActionResult> GetLowStockProducts([FromQuery] LowStockProductListQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        [HttpGet("Sale-POS")]
        public async Task<IActionResult> GetPosSaleProducts([FromQuery] ProductPosSaleQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }


        [HttpGet("Scan")]
        public async Task<IActionResult> Scan(
        [FromQuery] ProductScanQuery query,
        CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return this.ToActionResult(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("product:view")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new ProductGetByIdQuery { Id = id }, cancellationToken);
            return this.ToActionResult(result);
        }

        [HttpPost]
        [RequirePermission("product:create")]
        public async Task<IActionResult> Create(
            [FromBody] ProductCreateCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return this.ToActionResult(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("product:update")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] ProductUpdateCommand command,
            CancellationToken cancellationToken)
        {
            command.Id = id;
            var result = await _mediator.Send(command, cancellationToken);
            return this.ToActionResult(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("product:delete")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new ProductDeleteCommand { Id = id }, cancellationToken);
            return this.ToActionResult(result);
        }
    }
}