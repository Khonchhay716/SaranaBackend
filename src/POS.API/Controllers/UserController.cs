using Microsoft.AspNetCore.Mvc;
using MediatR;
using CoreAuthBackend.Client.Core.Models;
using CoreAuthBackend.Client.Controllers.Extensions;
using POS.Application.Features.User;

namespace POS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IMediator _medaitor;
        public UserController(IMediator mediator)
        {
            _medaitor = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<UserInfo>>>> GetUser([FromQuery] UserListquery query)
        {
            var result = await _medaitor.Send(query);
            return Ok(result.ToApiResponse("Get data is successfully !!!!"));
        }


        [HttpPost]
        public async Task<IActionResult> CreateUser(UserCreateCommand command)
        {
            var result = await _medaitor.Send(command);
            return this.ToActionResult(result);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var query = new UserQuery { Id = id };
            var result = await _medaitor.Send(query);
            return this.ToActionResult(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUnit(int id, [FromBody] UserUpdateCommand command)
        {
            command.Id = id;
            var updated = await _medaitor.Send(command);
            return this.ToActionResult(updated);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteUnit(int id)
        {
            var command = new UserDeleteCommand { Id = id };
            var response = await _medaitor.Send(command);
            if (response == null) return this.ApiNotFound($"Unit with id {id} was not found");
            return response;
        }
    }
}