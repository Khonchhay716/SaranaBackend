// POS.Application/Features/Book/CheckBookAvailabilityQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Book
{
    public class CheckBookAvailabilityQuery : IRequest<ApiResponse<BookAvailabilityInfo>>
    {
        public int Id { get; set; }
    }

    public class BookAvailabilityInfo
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int TotalQty { get; set; }
        public int AvailableQty { get; set; }
        public bool IsAvailable { get; set; }
        public string RackNo { get; set; } = string.Empty;
    }

    public class CheckBookAvailabilityQueryHandler : IRequestHandler<CheckBookAvailabilityQuery, ApiResponse<BookAvailabilityInfo>>
    {
        private readonly IMyAppDbContext _context;

        public CheckBookAvailabilityQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<BookAvailabilityInfo>> Handle(CheckBookAvailabilityQuery request, CancellationToken cancellationToken)
        {
            var book = await _context.Books
                .AsNoTracking()
                .Where(b => b.Id == request.Id && !b.IsDeleted)
                .Select(b => new BookAvailabilityInfo
                {
                    BookId = b.Id,
                    Title = b.Title,
                    TotalQty = b.TotalQty,
                    AvailableQty = b.AvailableQty,
                    IsAvailable = b.AvailableQty > 0,
                    RackNo = b.RackNo
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (book == null)
            {
                return ApiResponse<BookAvailabilityInfo>.NotFound($"Book with id {request.Id} was not found");
            }

            return ApiResponse<BookAvailabilityInfo>.Ok(book, "Book availability retrieved successfully");
        }
    }
}