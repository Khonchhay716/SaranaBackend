// POS.Application/Features/Book/GetBookQuery.cs
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Book
{
    public class GetBookQuery : IRequest<ApiResponse<BookInfo>>
    {
        public int Id { get; set; }
    }

    public class GetBookQueryHandler : IRequestHandler<GetBookQuery, ApiResponse<BookInfo>>
    {
        private readonly IMyAppDbContext _context;

        public GetBookQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<BookInfo>> Handle(GetBookQuery request, CancellationToken cancellationToken)
        {
            // Load book
            var book = await _context.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == request.Id && !b.IsDeleted, cancellationToken);

            if (book == null)
                return ApiResponse<BookInfo>.NotFound($"Book with id {request.Id} was not found");

            // Map to DTO
            var data = book.Adapt<BookInfo>();

            // Load category if exists
            if (book.CategoryId != null)
            {
                var category = await _context.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == book.CategoryId && !c.IsDeleted, cancellationToken);

                if (category != null)
                {
                    data.Category = new TypeNamebase
                    {
                        Id = category.Id,
                        Name = category.Name
                    };
                    data.CategoryId = category.Id;
                }
            }

            return ApiResponse<BookInfo>.Ok(data, "Book retrieved successfully");
        }
    }
}