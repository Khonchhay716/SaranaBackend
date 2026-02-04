using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.API.Extensions;
using POS.Application.Common.Dto;
using POS.Application.Features.User;
using System.Threading.Tasks;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> GetUsersLookup([FromQuery] UserLookupQuery query)
        {
            var result = await _mediator.Send(query);
            return this.ToActionResult(result);
        }

        [HttpGet]
        [RequirePermission("user:list")]
        public async Task<ActionResult<PaginatedResult<UserInfo>>> GetUsers([FromQuery] UserListQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("user:read")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var query = new GetUserByIdQuery(id);
            var result = await _mediator.Send(query);
            return this.ToActionResult(result);
        }

        [HttpPost]
        [RequirePermission("user:create")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
        {
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("user:update")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserCommand command)
        {
            command.UserId = id;
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("user:delete")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var command = new DeleteUserCommand(id);
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        [HttpPut("{id}/roles")]
        // [RequirePermission("user:assign-roles")]
        public async Task<IActionResult> AssignRolesToUser(int id, [FromBody] AssignRolesToUserCommand command)
        {
            command.UserId = id;
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        [HttpPut("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordCommand command)
        {
            command.UserId = id;
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }


        [HttpPut("update-password-by-email")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdatePasswordByEmail([FromBody] UpdatePasswordCommand command)
        {
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }
    }
}