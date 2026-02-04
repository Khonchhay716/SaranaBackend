// POS.Application/Features/Book/UpdateBookCommand.cs
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
    public class UpdateBookCommand : IRequest<ApiResponse<BookInfo>>
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string Author { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? ISBN { get; set; }
        public string? Publisher { get; set; }
        public string? Edition { get; set; }
        public DateTimeOffset PublishedYear { get; set; }
        public int TotalQty { get; set; }
        // public int AvailableQty { get; set; } // Keep this commented out so Mapster doesn't touch it
        public decimal Price { get; set; }
        public string RackNo { get; set; } = string.Empty;
        public string No { get; set; } = string.Empty;
    }

    public class UpdateBookCommandHandler : IRequestHandler<UpdateBookCommand, ApiResponse<BookInfo>>
    {
        private readonly IMyAppDbContext _context;

        public UpdateBookCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<BookInfo>> Handle(UpdateBookCommand request, CancellationToken cancellationToken)
        {
            // 1. Fetch the existing book
            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == request.Id && !b.IsDeleted, cancellationToken);

            if (book == null)
            {
                return ApiResponse<BookInfo>.NotFound($"Book with id {request.Id} was not found");
            }

            // ---------------------------------------------------------
            // THE FIX: Calculate Quantity Difference
            // ---------------------------------------------------------
            int oldTotalQty = book.TotalQty;
            int newTotalQty = request.TotalQty;
            int quantityDifference = newTotalQty - oldTotalQty;

            // Validation: Ensure we don't reduce total stock below the amount currently borrowed.
            // (Total - Available) = Currently Borrowed Count
            int currentlyBorrowed = oldTotalQty - book.AvailableQty;
            
            if (newTotalQty < currentlyBorrowed)
            {
                return ApiResponse<BookInfo>.BadRequest(
                    $"Cannot set Total Qty to {newTotalQty}. There are currently {currentlyBorrowed} books borrowed out. Minimum Total Qty allowed is {currentlyBorrowed}."
                );
            }

            // 2. Validate Category
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

            // 3. Update Properties
            // Mapster updates Title, Author, Price, etc. AND it updates TotalQty
            request.Adapt(book);

            // 4. Update AvailableQty
            // Apply the difference we calculated earlier
            book.AvailableQty += quantityDifference;

            book.UpdatedDate = DateTimeOffset.UtcNow;
            // book.UpdatedBy = ... (if you have current user service)

            await _context.SaveChangesAsync(cancellationToken);

            // 5. Prepare Response
            var data = book.Adapt<BookInfo>();
            data.Category = new TypeNamebase
            {
                Id = request.CategoryId,
                Name = categoryName!
            };

            return ApiResponse<BookInfo>.Ok(data, "Book updated successfully");
        }
    }
}