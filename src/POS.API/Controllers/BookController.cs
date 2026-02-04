// POS.API/Controllers/BookController.cs
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.API.Extensions;
using POS.Application.Common.Dto;
using POS.Application.Features.Book;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BookController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> GetBookLookup([FromQuery] BookLookupQuery query)
        {
            var result = await _mediator.Send(query);
            return this.ToActionResult(result);
        }


        [HttpGet]
        [RequirePermission("book:list")]
        public async Task<ActionResult<PaginatedResult<BookInfo>>> GetBooks([FromQuery] BookListQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("book:create")]
        public async Task<IActionResult> CreateBook([FromBody] CreateBookCommand command)
        {
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("book:read")]
        public async Task<IActionResult> GetBook(int id)
        {
            var query = new GetBookQuery { Id = id };
            var result = await _mediator.Send(query);
            return this.ToActionResult(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("book:update")]
        public async Task<IActionResult> UpdateBook(int id, [FromBody] UpdateBookCommand command)
        {
            command.Id = id;
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("book:delete")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var command = new DeleteBookCommand { Id = id };
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        [HttpPost("{id}/restore")]
        [RequirePermission("book:update")]
        public async Task<IActionResult> RestoreBook(int id)
        {
            var command = new RestoreBookCommand { Id = id };
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        [HttpGet("by-author/{author}")]
        [RequirePermission("book:read")]
        public async Task<ActionResult<PaginatedResult<BookInfo>>> GetBooksByAuthor(
            string author,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new BookListQuery
            {
                Search = author,
                Page = page,
                PageSize = pageSize
            };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("by-subject/{subject}")]
        [RequirePermission("book:read")]
        public async Task<ActionResult<PaginatedResult<BookInfo>>> GetBooksBySubject(
            string subject,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new BookListQuery
            {
                Search = subject,
                Page = page,
                PageSize = pageSize
            };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{id}/availability")]
        [RequirePermission("book:read")]
        public async Task<IActionResult> CheckAvailability(int id)
        {
            var query = new GetBookQuery { Id = id };
            var result = await _mediator.Send(query);
            return this.ToActionResult(result);
        }
    }
}