using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Extensions;
using POS.Application.Features.Product;

namespace POS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProductController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult> GetProducts([FromQuery] ProductListQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var query = new ProductQuery { Id = id };
            var result = await _mediator.Send(query);
            return this.ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateCommand command)
        {
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpdateCommand command)
        {
            command.Id = id;
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var command = new ProductDeleteCommand { Id = id };
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }
    }
}