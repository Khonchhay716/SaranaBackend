using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.API.Extensions;
using POS.Application.Common.Dto;
using POS.Application.Features.StockManagement.Suppliers;

namespace POS.API.Controllers.StockManagement
{
    [ApiController]
    [Route("api/[controller]")]
    public class SuppliersController : ControllerBase
    {
        private readonly IMediator _mediator;
        public SuppliersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("lookup")]
        public async Task<ActionResult<PaginatedResult<SupplierLookup>>> SupplierLookup([FromQuery] SupplierLookupQuery command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        [RequirePermission("supplier:read")]
        public async Task<ActionResult<PaginatedResult<SupplierInfo>>> GetList(
            [FromQuery] SupplierListQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("supplier:view")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new SupplierGetByIdQuery { Id = id }, cancellationToken);
            return this.ToActionResult(result);
        }

        [HttpPost]
        [RequirePermission("supplier:create")]
        public async Task<IActionResult> Create(
            [FromBody] SupplierCreateCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return this.ToActionResult(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("supplier:update")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] SupplierUpdateCommand command,
            CancellationToken cancellationToken)
        {
            command.Id = id;
            var result = await _mediator.Send(command, cancellationToken);
            return this.ToActionResult(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("supplier:delete")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new SupplierDeleteCommand { Id = id }, cancellationToken);
            return this.ToActionResult(result);
        }
    }
}