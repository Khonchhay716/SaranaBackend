// POS.Application/Features/Book/CreateBookCommand.cs
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Book
{
    public class CreateBookCommand : IRequest<ApiResponse<BookInfo>>
    {
        public string Title { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string Author { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? ISBN { get; set; }
        public string? Publisher { get; set; }
        public string? Edition { get; set; }
        public DateTimeOffset PublishedYear { get; set; }
        public int TotalQty { get; set; }
        // public int AvailableQty { get; set; }
        public decimal Price { get; set; }
        public string RackNo { get; set; } = string.Empty;
        public string No { get; set; } = string.Empty;
    }

    public class CreateBookCommandHandler : IRequestHandler<CreateBookCommand, ApiResponse<BookInfo>>
    {
        private readonly IMyAppDbContext _context;

        public CreateBookCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<BookInfo>> Handle(CreateBookCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return ApiResponse<BookInfo>.BadRequest("Title is required");
            }
            if (request.Title.Length < 3)
            {
                return ApiResponse<BookInfo>.BadRequest(
                    "Title must have at least 3 characters"
                );
            }
            string? categoryName = null;
            if (request.CategoryId != 0)
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == request.CategoryId && !c.IsDeleted, cancellationToken);

                if (category == null)
                {
                    return ApiResponse<BookInfo>.NotFound("Category not found");
                }

                if (!category.IsActive)
                {
                    return ApiResponse<BookInfo>.BadRequest("Category is not active");
                }

                categoryName = category.Name;
            }

            var book = request.Adapt<Domain.Entities.Book>();
            book.AvailableQty = request.TotalQty;
            book.CreatedDate = DateTimeOffset.UtcNow;
            book.UpdatedDate = DateTimeOffset.UtcNow;

            _context.Books.Add(book);
            await _context.SaveChangesAsync(cancellationToken);
            var data = book.Adapt<BookInfo>();
            data.Category = new TypeNamebase
            {
                Id = request.CategoryId,
                Name = categoryName!
            };
            return ApiResponse<BookInfo>.Created(data, "Book created successfully");
        }
    }
}