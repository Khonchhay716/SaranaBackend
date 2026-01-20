using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.API.Extensions;
using POS.Application.Common.Dto;
using POS.Application.Features.Role;
using System.Threading.Tasks;
using POS.Application.Features.Permission;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RolesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [RequirePermission("role:read")]
        public async Task<ActionResult<PaginatedResult<RoleInfo>>> GetRoles([FromQuery] RoleListQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("role:read")]
        public async Task<IActionResult> GetRoleById(int id)
        {
            var query = new GetRoleByIdQuery(id);
            var result = await _mediator.Send(query);

            if (result == null)
                return NotFound($"Role with ID {id} not found");

            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("role:create")]
        public async Task<IActionResult> CreateRole([FromBody] RoleCreateCommand command)
        {
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }
        
        [HttpPut("{id}")]
        [RequirePermission("role:update")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] RoleUpdateCommand command)
        {
            command.Id = id;
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        [HttpPut("{id}/permissions")]
        [RequirePermission("role:assign-permissions")]
        public async Task<IActionResult> UpdateRolePermissions(int id, [FromBody] AssignPermissionsToRoleCommand command)
        {
            command.RoleId = id;
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        [HttpGet("{id}/permissions")]
        [RequirePermission("role:read")]
        public async Task<IActionResult> GetRolePermissions(int id)
        {
            var query = new GetRolePermissionsQuery(id);
            var result = await _mediator.Send(query);

            if (result == null)
                return NotFound($"Role with ID {id} not found");

            return Ok(result);
        }
    }
}