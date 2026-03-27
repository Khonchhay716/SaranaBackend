using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.API.Extensions;
using POS.Application.Common.Dto;
using POS.Application.Features.Category;
 
namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly IMediator _mediator;
 
        public CategoryController(IMediator mediator)
        {
            _mediator = mediator;
        }
 
        [HttpGet("lookup")]
        public async Task<ActionResult<PaginatedResult<CategoryInfoLookup>>> GetCategoriesLookup([FromQuery] CategoryLookupListQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }
 
        [HttpGet]
        [RequirePermission("category:read")]
        public async Task<ActionResult<PaginatedResult<CategoryInfo>>> GetCategories([FromQuery] CategoryListQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }
 
        [HttpGet("{id}")]
        [RequirePermission("category:view")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var result = await _mediator.Send(new GetCategoryQuery { Id = id });
            return this.ToActionResult(result);
        }
 
        [HttpPost]
        [RequirePermission("category:create")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command)
        {
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }
 
        [HttpPut("{id}")]
        [RequirePermission("category:update")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryCommand command)
        {
            command.Id = id;
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }
 
        [HttpDelete("{id}")]
        [RequirePermission("category:delete")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var result = await _mediator.Send(new DeleteCategoryCommand { Id = id });
            return this.ToActionResult(result);
        }
    }
}