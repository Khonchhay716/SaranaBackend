// POS.API/Controllers/LibraryMemberController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.API.Extensions;
using POS.Application.Common.Dto;
using POS.Application.Features.LibraryMember;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LibraryMemberController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LibraryMemberController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> GetMembersLookup([FromQuery] LibraryMemberLookupQuery query)
        {
            var result = await _mediator.Send(query);
            return this.ToActionResult(result);
        }

        /// <summary>
        /// Get all library members with optional filters (status, active, search)
        /// </summary>
        [HttpGet]
        [RequirePermission("librarymember:read")]
        public async Task<ActionResult<PaginatedResult<LibraryMemberInfo>>> GetMembers([FromQuery] LibraryMemberListQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get only approved library members
        /// </summary>
        [HttpGet("approved")]
        [RequirePermission("librarymember:list")]
        public async Task<ActionResult<PaginatedResult<LibraryMemberInfo>>> GetApprovedMembers([FromQuery] GetApprovedLibraryMembersQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get a single library member by ID
        /// </summary>
        [HttpGet("{id}")]
        [RequirePermission("librarymember:read")]
        public async Task<IActionResult> GetMember(int id)
        {
            var query = new GetLibraryMemberQuery { Id = id };
            var result = await _mediator.Send(query);
            return this.ToActionResult(result);
        }

        /// <summary>
        /// Create a new library member
        /// </summary>
        [HttpPost]
        [RequirePermission("librarymember:create")]
        public async Task<IActionResult> CreateMember([FromBody] CreateLibraryMemberCommand command)
        {
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        /// <summary>
        /// Update an existing library member
        /// </summary>
        [HttpPut("{id}")]
        [RequirePermission("librarymember:update")]
        public async Task<IActionResult> UpdateMember(int id, [FromBody] UpdateLibraryMemberCommand command)
        {
            command.Id = id;
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }
        [HttpGet("CureentUser")]
        [RequirePermission("librarymember:request")]
        public async Task<ActionResult<PaginatedResult<LibraryMemberInfo>>> GetMyMembership([FromQuery] GetCurrentUserLibraryMemberQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }


        /// <summary>
        /// Approve a library member
        /// </summary>
        [HttpPost("{id}/approve")]
        [RequirePermission("librarymember:approve")]
        public async Task<IActionResult> ApproveMember(int id)
        {
            var command = new ApproveLibraryMemberCommand { Id = id };
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        /// <summary>
        /// Reject a library member
        /// </summary>
        [HttpPost("{id}/reject")]
        [RequirePermission("librarymember:reject")]
        public async Task<IActionResult> RejectMember(int id, [FromBody] RejectLibraryMemberCommand command)
        {
            command.Id = id;
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        /// <summary>
        /// Cancel a library member
        /// </summary>
        [HttpPost("{id}/cancel")]
        [RequirePermission("librarymember:cancel")]
        public async Task<IActionResult> CancelMember(int id, [FromBody] CancelLibraryMemberCommand command)
        {
            command.Id = id;
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        /// <summary>
        /// Delete (soft delete) a library member
        /// </summary>
        [HttpDelete("{id}")]
        [RequirePermission("librarymember:delete")]
        public async Task<IActionResult> DeleteMember(int id)
        {
            var command = new DeleteLibraryMemberCommand { Id = id };
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }
    }
}