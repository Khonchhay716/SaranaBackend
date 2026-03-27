// using FluentValidation;
// using Mapster;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Dto;
// using POS.Application.Common.Interfaces;
// using POS.Application.Common.Typebase;
// using DomainSerialNumber = POS.Domain.Entities.SerialNumber;
// using DomainStockMovement = POS.Domain.Entities.StockMovement;

// namespace POS.Application.Features.Product
// {
//     public class SerialNumberUpdateItem
//     {
//         public int Id { get; set; }
//         public string SerialNo { get; set; } = string.Empty;
//     }

//     public record ProductUpdateCommand : IRequest<ApiResponse<ProductInfo>>
//     {
//         public int Id { get; set; }
//         public string Name { get; set; } = string.Empty;
//         public string? Description { get; set; }
//         public string? SKU { get; set; }
//         public string? Barcode { get; set; }
//         public decimal Price { get; set; }
//         public decimal? CostPrice { get; set; }
//         public int? CategoryId { get; set; }
//         public string? ImageProduct { get; set; }
//         public bool IsSerialNumber { get; set; }
//         public int MinStock { get; set; } = 0;
//         public int? BranchId { get; set; }
//         public string? RAM { get; set; }
//         public string? Storage { get; set; }
//         public List<string> SerialNumbersToAdd { get; set; } = new();
//         public List<int> SerialNumberIdsToRemove { get; set; } = new();
//         public List<SerialNumberUpdateItem> SerialNumbersToUpdate { get; set; } = new();

//         public int StockToAdd { get; set; } = 0;
//         public int StockToRemove { get; set; } = 0;
//         public string? StockNotes { get; set; }
//     }

//     public class ProductUpdateCommandValidator : AbstractValidator<ProductUpdateCommand>
//     {
//         public ProductUpdateCommandValidator()
//         {
//             RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
//             RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
//             RuleFor(x => x.StockToAdd).GreaterThanOrEqualTo(0);
//             RuleFor(x => x.StockToRemove).GreaterThanOrEqualTo(0);
//             RuleFor(x => x.MinStock).GreaterThanOrEqualTo(0);
//         }
//     }

//     public class ProductUpdateCommandHandler : IRequestHandler<ProductUpdateCommand, ApiResponse<ProductInfo>>
//     {
//         private readonly IMyAppDbContext _context;

//         public ProductUpdateCommandHandler(IMyAppDbContext context)
//         {
//             _context = context;
//         }

//         public async Task<ApiResponse<ProductInfo>> Handle(ProductUpdateCommand request, CancellationToken cancellationToken)
//         {
//             // 1. Fetch
//             var product = await _context.Products
//                 .Include(p => p.SerialNumbers.Where(s => !s.IsDeleted))
//                 .Include(p => p.StockMovements.Where(sm => !sm.IsDeleted))
//                 .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

//             if (product == null)
//                 return ApiResponse<ProductInfo>.NotFound($"Product {request.Id} not found");

//             // 2. Validate Category
//             if (request.CategoryId.HasValue)
//             {
//                 var categoryExists = await _context.Categories
//                     .AnyAsync(c => c.Id == request.CategoryId && !c.IsDeleted, cancellationToken);
//                 if (!categoryExists)
//                     return ApiResponse<ProductInfo>.BadRequest("Invalid Category");
//             }

//             // 3. Validate Branch
//             if (request.BranchId.HasValue)
//             {
//                 var branchExists = await _context.Branches
//                     .AnyAsync(b => b.Id == request.BranchId.Value && !b.IsDeleted, cancellationToken);
//                 if (!branchExists)
//                     return ApiResponse<ProductInfo>.BadRequest("Invalid Branch");
//             }

//             // 4. Basic Field Updates
//             product.Name = request.Name;
//             product.Description = request.Description;
//             product.SKU = request.SKU;
//             product.Barcode = request.Barcode;
//             product.Price = request.Price;
//             product.CostPrice = request.CostPrice;
//             product.CategoryId = request.CategoryId;
//             product.ImageProduct = request.ImageProduct;
//             product.IsSerialNumber = request.IsSerialNumber;
//             product.MinStock = request.MinStock;
//             product.BranchId = request.BranchId;
//             product.RAM = request.RAM;
//             product.Storage = request.Storage;
//             product.UpdatedDate = DateTimeOffset.UtcNow;

//             if (request.IsSerialNumber)
//             {
//                 if (request.SerialNumberIdsToRemove.Any())
//                 {
//                     var toRemove = product.SerialNumbers
//                         .Where(s => request.SerialNumberIdsToRemove.Contains(s.Id)).ToList();
//                     foreach (var sn in toRemove) { sn.IsDeleted = true; sn.DeletedDate = DateTimeOffset.UtcNow; }
//                 }

//                 foreach (var item in request.SerialNumbersToUpdate)
//                 {
//                     var sn = product.SerialNumbers.FirstOrDefault(s => s.Id == item.Id);
//                     if (sn != null) { sn.SerialNo = item.SerialNo; sn.UpdatedDate = DateTimeOffset.UtcNow; }
//                 }

//                 foreach (var snToAdd in request.SerialNumbersToAdd)
//                 {
//                     product.SerialNumbers.Add(new DomainSerialNumber
//                     {
//                         SerialNo = snToAdd,
//                         Status = "Available",
//                         ProductId = product.Id,
//                         Price = request.Price,
//                         CostPrice = request.CostPrice ?? 0,
//                     });
//                 }

//                 product.Stock = product.SerialNumbers.Count(s => !s.IsDeleted && s.Status != "Sold");
//             }
//             else
//             {
//                 if (request.StockToAdd > 0)
//                 {
//                     product.StockMovements.Add(new DomainStockMovement
//                     {
//                         ProductId = product.Id,
//                         Type = "StockIn",
//                         Quantity = request.StockToAdd,
//                         Price = request.Price,
//                         CostPrice = request.CostPrice ?? 0,
//                         Notes = request.StockNotes,
//                         MovementDate = DateTimeOffset.UtcNow,
//                     });
//                     product.Stock += request.StockToAdd;
//                 }

//                 if (request.StockToRemove > 0)
//                 {
//                     if (request.StockToRemove > product.Stock)
//                         return ApiResponse<ProductInfo>.BadRequest(
//                             $"Cannot remove {request.StockToRemove} units. Only {product.Stock} in stock.");

//                     product.StockMovements.Add(new DomainStockMovement
//                     {
//                         ProductId = product.Id,
//                         Type = "StockOut",
//                         Quantity = request.StockToRemove,
//                         Price = request.Price,
//                         CostPrice = request.CostPrice ?? 0,
//                         Notes = request.StockNotes,
//                         MovementDate = DateTimeOffset.UtcNow,
//                     });
//                     product.Stock -= request.StockToRemove;
//                 }
//             }

//             // 5. Save
//             await _context.SaveChangesAsync(cancellationToken);

//             // 6. Reload with navigations
//             var updatedProduct = await _context.Products
//                 .Include(p => p.Category)
//                 .Include(p => p.Branch)         
//                 .Include(p => p.SerialNumbers.Where(s => !s.IsDeleted))
//                 .Include(p => p.StockMovements.Where(sm => !sm.IsDeleted))
//                 .AsNoTracking()
//                 .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

//             var productInfo = updatedProduct!.Adapt<ProductInfo>();

//             if (updatedProduct.Category != null)
//                 productInfo.Category = new TypeNamebase { Id = updatedProduct.Category.Id, Name = updatedProduct.Category.Name };

//             if (updatedProduct.Branch != null)                                  
//                 productInfo.Branch = new TypeNamebase { Id = updatedProduct.Branch.Id, Name = updatedProduct.Branch.BranchName };

//             return ApiResponse<ProductInfo>.Ok(productInfo, "Product updated successfully");
//         }
//     }
// }



// POS.Application/Features/Product/ProductUpdateCommand.cs
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;

namespace POS.Application.Features.Product
{
    public record ProductUpdateCommand : IRequest<ApiResponse<ProductInfo>>
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public int      Id            { get; set; }
        public string   Name          { get; set; } = string.Empty;
        public string?  Description   { get; set; }
        public string?  SKU           { get; set; }
        public string?  Barcode       { get; set; }
        public decimal  Price         { get; set; }
        public decimal? CostPrice     { get; set; }
        public int?     CategoryId    { get; set; }
        public string?  ImageProduct  { get; set; }
        public bool     IsSerialNumber { get; set; }
        public int      MinStock      { get; set; } = 0;
        public int?     BranchId      { get; set; }
        public string?  RAM           { get; set; }
        public string?  Storage       { get; set; }
    }

    public class ProductUpdateCommandValidator : AbstractValidator<ProductUpdateCommand>
    {
        public ProductUpdateCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MinStock).GreaterThanOrEqualTo(0);
        }
    }

    public class ProductUpdateCommandHandler : IRequestHandler<ProductUpdateCommand, ApiResponse<ProductInfo>>
    {
        private readonly IMyAppDbContext _context;

        public ProductUpdateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<ProductInfo>> Handle(ProductUpdateCommand request, CancellationToken cancellationToken)
        {
            // 1. Fetch product only — no Include SNs/SMs needed
            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (product == null)
                return ApiResponse<ProductInfo>.NotFound($"Product {request.Id} not found");

            // 2. Validate Category
            if (request.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories
                    .AnyAsync(c => c.Id == request.CategoryId && !c.IsDeleted, cancellationToken);
                if (!categoryExists)
                    return ApiResponse<ProductInfo>.BadRequest("Invalid Category");
            }

            // 3. Validate Branch
            if (request.BranchId.HasValue)
            {
                var branchExists = await _context.Branches
                    .AnyAsync(b => b.Id == request.BranchId.Value && !b.IsDeleted, cancellationToken);
                if (!branchExists)
                    return ApiResponse<ProductInfo>.BadRequest("Invalid Branch");
            }

            // 4. Update product fields only
            product.Name           = request.Name;
            product.Description    = request.Description;
            product.SKU            = request.SKU;
            product.Barcode        = request.Barcode;
            product.Price          = request.Price;
            product.CostPrice      = request.CostPrice;
            product.CategoryId     = request.CategoryId;
            product.ImageProduct   = request.ImageProduct;
            product.IsSerialNumber = request.IsSerialNumber;
            product.MinStock       = request.MinStock;
            product.BranchId       = request.BranchId;
            product.RAM            = request.RAM;
            product.Storage        = request.Storage;
            product.UpdatedDate    = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // 5. Reload with navigations
            var updated = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Branch)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            var productInfo = updated!.Adapt<ProductInfo>();

            if (updated.Category != null)
                productInfo.Category = new TypeNamebase { Id = updated.Category.Id, Name = updated.Category.Name };
            if (updated.Branch != null)
                productInfo.Branch = new TypeNamebase { Id = updated.Branch.Id, Name = updated.Branch.BranchName };

            productInfo.SerialNumbers  = new List<SerialNumberInfo>();
            productInfo.StockMovements = new List<StockMovementInfo>();

            return ApiResponse<ProductInfo>.Ok(productInfo, "Product updated successfully");
        }
    }
}