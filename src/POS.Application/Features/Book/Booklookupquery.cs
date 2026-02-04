// POS.Application/Features/Book/BookLookupQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Book
{
    // Lookup DTO - lightweight for dropdowns
    public class BookLookupDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public int AvailableQty { get; set; }
        public decimal Price { get; set; }
    }

    // Query Request
    public class BookLookupQuery : IRequest<ApiResponse<List<BookLookupDto>>>
    {
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public bool OnlyAvailable { get; set; } = false; // Filter books with available quantity > 0
    }

    // Query Handler
    public class BookLookupQueryHandler : IRequestHandler<BookLookupQuery, ApiResponse<List<BookLookupDto>>>
    {
        private readonly IMyAppDbContext _context;

        public BookLookupQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<BookLookupDto>>> Handle(BookLookupQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Books
                .Where(b => !b.IsDeleted)
                .AsNoTracking();

            // Filter by available quantity
            if (request.OnlyAvailable)
            {
                query = query.Where(b => b.AvailableQty > 0);
            }

            // Filter by category
            if (request.CategoryId.HasValue)
            {
                query = query.Where(b => b.CategoryId == request.CategoryId.Value);
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(b =>
                    b.Title.Contains(request.Search) ||
                    b.Author.Contains(request.Search) ||
                    b.ISBN.Contains(request.Search) ||
                    b.Subject.Contains(request.Search)
                );
            }

            // Order by title and take top 100 for performance
            var books = await query
                .OrderBy(b => b.Title)
                .Take(100)
                .Select(b => new BookLookupDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    ISBN = b.ISBN,
                    AvailableQty = b.AvailableQty,
                    Price = b.Price
                })
                .ToListAsync(cancellationToken);

            return ApiResponse<List<BookLookupDto>>.Ok(books);
        }
    }
}