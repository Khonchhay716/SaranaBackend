// POS.API/Controllers/BookIssueController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.API.Extensions;
using POS.Application.Common.Dto;
using POS.Application.Features.BookIssue;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookIssueController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BookIssueController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [RequirePermission("bookissue:list")]
        public async Task<ActionResult<PaginatedResult<BookIssueInfo>>> GetBookIssues([FromQuery] BookIssueListQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("my-pending-returns")]
        [RequirePermission("bookissue:currentuser")]
        public async Task<ActionResult<PaginatedResult<BookIssueInfo>>> GetMyPendingReturns([FromQuery] GetPendingReturnBooksQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("pending-returns")]
        [RequirePermission("bookissue:list")]
        public async Task<ActionResult<PaginatedResult<BookIssueInfo>>> GetAllPendingReturns([FromQuery] GetAllPendingReturnBooksQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("bookissue:create")]
        public async Task<IActionResult> IssueBook([FromBody] IssueBookCommand command)
        {
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("bookissue:read")]
        public async Task<IActionResult> GetBookIssue(int id)
        {
            var query = new GetBookIssueQuery { Id = id };
            var result = await _mediator.Send(query);
            return this.ToActionResult(result);
        }

        [HttpPost("{id}/return")]
        [RequirePermission("bookissue:update")]
        public async Task<IActionResult> ReturnBook(int id, [FromBody] ReturnBookCommand command)
        {
            command.Id = id;
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        [HttpGet("my-book-issues")]
        [RequirePermission("bookissue:currentuser")]
        public async Task<ActionResult<PaginatedResult<BookIssueInfo>>> GetCurrentUserBookIssues([FromQuery] GetCurrentUserBookIssuesQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        // Controller
        [HttpPost("{id}/renew")]
        [RequirePermission("bookissue:update")]
        public async Task<IActionResult> RenewBook(int id, [FromBody] RenewBookCommand command)
        {
            command.Id = id;
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("bookissue:delete")]
        public async Task<IActionResult> DeleteBookIssue(int id)
        {
            var command = new DeleteBookIssueCommand { Id = id };
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }
    }
}