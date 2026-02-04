using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Book
{
    public class BookListQuery : PaginationRequest, IRequest<PaginatedResult<BookInfo>>
    {
        public string? Search { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
    }

    public class BookListQueryHandler : IRequestHandler<BookListQuery, PaginatedResult<BookInfo>>
    {
        private readonly IMyAppDbContext _context;

        public BookListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<BookInfo>> Handle(BookListQuery request, CancellationToken cancellationToken)
        {
            // Query Books, not Coupons
            var query = _context.Books.AsNoTracking().Where(x => !x.IsDeleted);

            // Search filter - search across multiple book fields
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(b =>
                    b.Title.Contains(request.Search) ||
                    b.Author.Contains(request.Search) ||
                    b.ISBN.Contains(request.Search) ||
                    b.Subject.Contains(request.Search)
                );
            }

            // Date range filters
            if (request.StartDate.HasValue)
            {
                query = query.Where(b => b.PublishedYear >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(b => b.PublishedYear <= request.EndDate.Value);
            }

            // Order by most recent first
            query = query.OrderByDescending(b => b.PublishedYear);

            // Project to BookInfo
            var projectedQuery = query.Select(b => new BookInfo
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Subject = b.Subject,
                ISBN = b.ISBN,
                Publisher = b.Publisher,
                Edition = b.Edition,
                PublishedYear = b.PublishedYear,
                TotalQty = b.TotalQty,
                AvailableQty = b.AvailableQty,
                Price = b.Price,
                RackNo = b.RackNo,
                No = b.No,
                CategoryId = b.CategoryId,
                Category = b.Category == null ? null : new TypeNamebase
                {
                    Id = b.Category.Id,
                    Name = b.Category.Name
                }
            });

            return await projectedQuery.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }

}