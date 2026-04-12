// using FluentValidation;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Dto;
// using POS.Application.Common.Interfaces;
// using POS.Application.Common.Typebase;
// using POS.Domain.Enums;

// using DomainOrder = POS.Domain.Entities.Order;
// using DomainOrderItem = POS.Domain.Entities.OrderItem;
// using DomainProduct = POS.Domain.Entities.Product;
// using DomainSerialNumber = POS.Domain.Entities.SerialNumber;
// using DomainDiscount = POS.Domain.Entities.Discount;

// namespace POS.Application.Features.Order
// {
//     public class OrderItemCreateDto
//     {
//         public int ProductId { get; set; }
//         public List<int>? SerialNumberIds { get; set; }
//         public int Quantity { get; set; }
//         public decimal UnitPrice { get; set; }
//         public string? Notes { get; set; }
//         public int? WarrantyMonths { get; set; }
//     }

//     public record OrderCreateCommand : IRequest<ApiResponse<OrderCreateResponse>>
//     {
//         public int? CustomerId { get; set; }
//         public decimal? DiscountAmount { get; set; }
//         public SaleType? SaleType { get; set; }
//         public OrderStatus? Status { get; set; }
//         public PaymentStatus? PaymentStatus { get; set; }
//         public PaymentMethodCode? PaymentMethod { get; set; }
//         public string? Notes { get; set; }
//         public List<OrderItemCreateDto> Items { get; set; } = new();
//     }

//     internal sealed class PendingOrderItem
//     {
//         public DomainProduct Product { get; set; } = null!;
//         public OrderItemCreateDto Request { get; set; } = null!;
//         public DomainSerialNumber? Serial { get; set; }
//         public decimal ItemSubtotal { get; set; }
//         public decimal ItemTax { get; set; }
//         public decimal ItemTotalBeforeDiscount { get; set; }
//     }

//     public class OrderCreateCommandHandler : IRequestHandler<OrderCreateCommand, ApiResponse<OrderCreateResponse>>
//     {
//         private readonly IMyAppDbContext _context;
//         private readonly ICurrentUserService _currentUser;
//         public OrderCreateCommandHandler(IMyAppDbContext context, ICurrentUserService currentUser)
//         {
//             _context = context;
//             _currentUser = currentUser;
//         }

//         public async Task<ApiResponse<OrderCreateResponse>> Handle(OrderCreateCommand request, CancellationToken cancellationToken)
//         {
//             var now = DateTimeOffset.UtcNow;
//             var pendingItems = new List<PendingOrderItem>();

//             decimal runningSubtotal = 0;
//             decimal runningTax = 0;

//             foreach (var itemReq in request.Items)
//             {
//                 var prod = await _context.Products
//                     .Include(p => p.SerialNumbers.Where(s => !s.IsDeleted))
//                     .Include(p => p.ProductDiscounts.Where(pd => !pd.IsDeleted))
//                     .FirstOrDefaultAsync(p => p.Id == itemReq.ProductId && !p.IsDeleted, cancellationToken);

//                 if (prod == null) return ApiResponse<OrderCreateResponse>.NotFound($"Product {itemReq.ProductId} not found");

//                 int qty = prod.IsSerialNumber ? (itemReq.SerialNumberIds?.Count ?? 0) : itemReq.Quantity;

//                 if (prod.IsSerialNumber)
//                 {
//                     foreach (var sId in itemReq.SerialNumberIds?.Distinct() ?? new List<int>())
//                     {
//                         var serial = prod.SerialNumbers.FirstOrDefault(s => s.Id == sId);
//                         if (serial == null || serial.Status != "Available") continue;

//                         serial.Status = "Sold";
//                         serial.SoldDate = now;
//                         prod.Stock -= 1;

//                         decimal sub = itemReq.UnitPrice;
//                         decimal tax = sub * (prod.TaxRate / 100m);

//                         runningSubtotal += sub;
//                         runningTax += tax;

//                         pendingItems.Add(new PendingOrderItem
//                         {
//                             Product = prod,
//                             Request = itemReq,
//                             Serial = serial,
//                             ItemSubtotal = sub,
//                             ItemTax = tax,
//                             ItemTotalBeforeDiscount = sub + tax
//                         });
//                     }
//                 }
//                 else
//                 {
//                     if (prod.Stock < itemReq.Quantity) return ApiResponse<OrderCreateResponse>.BadRequest($"Stock low for {prod.Name}");
//                     prod.Stock -= itemReq.Quantity;

//                     decimal sub = itemReq.Quantity * itemReq.UnitPrice;
//                     decimal tax = sub * (prod.TaxRate / 100m);

//                     runningSubtotal += sub;
//                     runningTax += tax;

//                     pendingItems.Add(new PendingOrderItem
//                     {
//                         Product = prod,
//                         Request = itemReq,
//                         ItemSubtotal = sub,
//                         ItemTax = tax,
//                         ItemTotalBeforeDiscount = sub + tax
//                     });
//                 }
//             }

//             decimal runningTotalWithTax = runningSubtotal + runningTax;

//             var allActiveDiscounts = await _context.Discounts
//                 .Include(d => d.ProductDiscounts)
//                 .Where(d => !d.IsDeleted && d.IsActive
//                       && (d.StartDate == null || d.StartDate <= now)
//                       && (d.EndDate == null || d.EndDate >= now))
//                 .ToListAsync(cancellationToken);

//             decimal totalAutoDiscount = 0;

//             var applicableDiscounts = allActiveDiscounts
//                 .Where(d => d.MinOrderAmount == null || runningTotalWithTax >= d.MinOrderAmount)
//                 .ToList();

//             foreach (var d in applicableDiscounts)
//             {
//                 var specificProductIds = d.ProductDiscounts.Where(pd => !pd.IsDeleted).Select(pd => pd.ProductId).ToList();

//                 if (specificProductIds.Any())
//                 {
//                     decimal eligibleTotalWithTax = pendingItems
//                         .Where(p => specificProductIds.Contains(p.Product.Id))
//                         .Sum(p => p.ItemTotalBeforeDiscount);

//                     totalAutoDiscount += (d.Type == "Percentage")
//                         ? eligibleTotalWithTax * (d.Value / 100m)
//                         : d.Value;
//                 }
//                 else
//                 {
//                     totalAutoDiscount += (d.Type == "Percentage")
//                         ? runningTotalWithTax * (d.Value / 100m)
//                         : d.Value;
//                 }
//             }

//             decimal finalDiscountTotal = Math.Min(totalAutoDiscount + (request.DiscountAmount ?? 0), runningTotalWithTax);
//             decimal discountRate = runningTotalWithTax > 0 ? finalDiscountTotal / runningTotalWithTax : 0;
//             var orderItems = new List<DomainOrderItem>();

//             foreach (var pending in pendingItems)
//             {
//                 decimal itemDiscount = Math.Round(pending.ItemTotalBeforeDiscount * discountRate, 2);

//                 orderItems.Add(new DomainOrderItem
//                 {
//                     ProductId = pending.Product.Id,
//                     ImageProduct = pending.Product.ImageProduct,
//                     SerialNumberId = pending.Serial?.Id,
//                     Quantity = pending.Serial != null ? 1 : pending.Request.Quantity,
//                     UnitPrice = pending.Request.UnitPrice,
//                     SubTotal = pending.ItemSubtotal,
//                     DiscountAmount = itemDiscount,
//                     WarrantyMonths = pending.Request.WarrantyMonths,
//                     WarrantyStartDate = pending.Request.WarrantyMonths > 0 ? now : null,
//                     WarrantyEndDate = pending.Request.WarrantyMonths > 0 ? now.AddMonths(pending.Request.WarrantyMonths.Value) : null
//                 });
//             }
//             if (orderItems.Any())
//             {
//                 var diff = finalDiscountTotal - orderItems.Sum(i => i.DiscountAmount);
//                 orderItems.Last().DiscountAmount += diff;
//             }
//             var order = new DomainOrder
//             {
//                 OrderNumber = $"ORD-{now:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
//                 OrderDate = now,
//                 CustomerId = request.CustomerId,
//                 StaffId = _currentUser.UserId,
//                 SubTotal = runningSubtotal,
//                 TaxAmount = runningTax,
//                 DiscountAmount = finalDiscountTotal,
//                 TotalAmount = runningTotalWithTax - finalDiscountTotal,
//                 Status = request.Status ?? 0,
//                 SaleType = request.SaleType ?? 0,
//                 PaymentStatus = request.PaymentStatus ?? 0,
//                 PaymentMethod = request.PaymentMethod,
//                 OrderItems = orderItems,
//                 Notes = request.Notes,
//             };

//             _context.Orders.Add(order);
//             await _context.SaveChangesAsync(cancellationToken);

//             return ApiResponse<OrderCreateResponse>.Created(await GetOrderInfo(order.Id, cancellationToken), "Order Created (Discount on Total)");
//         }

//         private async Task<OrderCreateResponse> GetOrderInfo(int orderId, CancellationToken cancellationToken)
//         {
//             var o = await _context.Orders
//                 .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
//                 .Include(o => o.OrderItems).ThenInclude(oi => oi.SerialNumber)
//                 .AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
//             var staff = o!.StaffId.HasValue
//                 ? await _context.Persons
//                     .AsNoTracking()
//                     .Where(p => p.Id == o.StaffId)
//                     .Select(p => new TypeNamebase
//                     {
//                         Id = p.Id,
//                         Name = p.Username ?? "N/A"
//                     })
//                     .FirstOrDefaultAsync(cancellationToken)
//                 : null;

//             return new OrderCreateResponse
//             {
//                 Id = o!.Id,
//                 StaffId = o.StaffId,
//                 Staff = staff,
//                 OrderNumber = o.OrderNumber,
//                 OrderDate = o.OrderDate,
//                 SubTotal = o.SubTotal,
//                 TaxAmount = o.TaxAmount,
//                 TotalAmount = o.TotalAmount,
//                 DiscountAmount = o.DiscountAmount,
//                 PaymentMethod = new TypeNamebase
//                 {
//                     Id = (int)(o.PaymentMethod ?? 0),
//                     Name = o.PaymentMethod.ToString() ?? "N/A"
//                 },
//                 PaymentStatus = new TypeNamebase
//                 {
//                     Id = (int)o.PaymentStatus,
//                     Name = o.PaymentStatus.ToString()
//                 },

//                 SaleType = new TypeNamebase
//                 {
//                     Id = (int)o.SaleType,
//                     Name = o.SaleType.ToString()
//                 },
//                 Status = new TypeNamebase
//                 {
//                     Id = (int)o.Status,
//                     Name = o.Status.ToString(),
//                 },
//                 Notes = o.Notes,
//                 OrderItems = o.OrderItems.Select(oi => new OrderItemInfo
//                 {
//                     Id = oi.Id,
//                     OrderId = oi.OrderId,
//                     ProductId = oi.ProductId,
//                     ImageProduct = oi.ImageProduct ?? "",
//                     ProductName = oi.Product.Name,
//                     Quantity = oi.Quantity,
//                     UnitPrice = oi.UnitPrice,
//                     SubTotal = oi.SubTotal,
//                     SerialNumberId = oi.SerialNumberId,
//                     SerialNo = oi.SerialNumber != null ? new TypeNamebase
//                     {
//                         Id = (int)(oi.SerialNumberId ?? 0),
//                         Name = oi.SerialNumber?.SerialNo ?? "N/A"
//                     } : null,
//                     WarrantyMonths = oi.WarrantyMonths,
//                     WarrantyStartDate = oi.WarrantyStartDate,
//                     WarrantyEndDate = oi.WarrantyEndDate
//                 }).ToList()
//             };
//         }
//     }
// }



// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Dto;
// using POS.Application.Common.Interfaces;
// using POS.Application.Common.Typebase;
// using POS.Domain.Enums;

// using DomainOrder = POS.Domain.Entities.Order;
// using DomainOrderItem = POS.Domain.Entities.OrderItem;
// using DomainProduct = POS.Domain.Entities.Product;
// using DomainSerialNumber = POS.Domain.Entities.SerialNumber;

// namespace POS.Application.Features.Order
// {
//     public class OrderItemCreateDto
//     {
//         public int ProductId { get; set; }
//         public List<int>? SerialNumberIds { get; set; }
//         public int Quantity { get; set; }
//         public decimal UnitPrice { get; set; }
//         public string? Notes { get; set; }
//         public int? WarrantyMonths { get; set; }
//     }

//     public record OrderCreateCommand : IRequest<ApiResponse<OrderCreateResponse>>
//     {
//         public int? CustomerId { get; set; }
//         public decimal? DiscountAmount { get; set; }
//         public SaleType? SaleType { get; set; }
//         public OrderStatus? Status { get; set; }
//         public PaymentStatus? PaymentStatus { get; set; }
//         public PaymentMethodCode? PaymentMethod { get; set; }
//         public string? Notes { get; set; }
//         public int PointsUsed { get; set; } = 0;
//         public List<OrderItemCreateDto> Items { get; set; } = new();
//     }

//     internal sealed class PendingOrderItem
//     {
//         public DomainProduct Product { get; set; } = null!;
//         public OrderItemCreateDto Request { get; set; } = null!;
//         public DomainSerialNumber? Serial { get; set; }
//         public decimal ItemSubtotal { get; set; }
//         public decimal ItemTax { get; set; }
//         public decimal ItemTotalBeforeDiscount { get; set; }
//     }

//     public class OrderCreateCommandHandler : IRequestHandler<OrderCreateCommand, ApiResponse<OrderCreateResponse>>
//     {
//         private readonly IMyAppDbContext _context;
//         private readonly ICurrentUserService _currentUser;

//         public OrderCreateCommandHandler(IMyAppDbContext context, ICurrentUserService currentUser)
//         {
//             _context = context;
//             _currentUser = currentUser;
//         }

//         public async Task<ApiResponse<OrderCreateResponse>> Handle(
//             OrderCreateCommand request,
//             CancellationToken cancellationToken)
//         {
//             var now = DateTimeOffset.UtcNow;
//             var pendingItems = new List<PendingOrderItem>();
//             decimal runningSubtotal = 0;
//             decimal runningTax = 0;

//             // ==================== Build Items ====================
//             foreach (var itemReq in request.Items)
//             {
//                 var prod = await _context.Products
//                     .Include(p => p.SerialNumbers.Where(s => !s.IsDeleted))
//                     .Include(p => p.ProductDiscounts.Where(pd => !pd.IsDeleted))
//                     .FirstOrDefaultAsync(p => p.Id == itemReq.ProductId && !p.IsDeleted, cancellationToken);

//                 if (prod == null)
//                     return ApiResponse<OrderCreateResponse>.NotFound($"Product {itemReq.ProductId} not found");

//                 if (prod.IsSerialNumber)
//                 {
//                     foreach (var sId in itemReq.SerialNumberIds?.Distinct() ?? new List<int>())
//                     {
//                         var serial = prod.SerialNumbers.FirstOrDefault(s => s.Id == sId);
//                         if (serial == null || serial.Status != "Available") continue;

//                         serial.Status = "Sold";
//                         serial.SoldDate = now;
//                         prod.Stock -= 1;

//                         decimal sub = itemReq.UnitPrice;
//                         decimal tax = sub * (prod.TaxRate / 100m);
//                         runningSubtotal += sub;
//                         runningTax += tax;

//                         pendingItems.Add(new PendingOrderItem
//                         {
//                             Product = prod,
//                             Request = itemReq,
//                             Serial = serial,
//                             ItemSubtotal = sub,
//                             ItemTax = tax,
//                             ItemTotalBeforeDiscount = sub + tax
//                         });
//                     }
//                 }
//                 else
//                 {
//                     if (prod.Stock < itemReq.Quantity)
//                         return ApiResponse<OrderCreateResponse>.BadRequest($"Stock low for {prod.Name}");

//                     prod.Stock -= itemReq.Quantity;

//                     decimal sub = itemReq.Quantity * itemReq.UnitPrice;
//                     decimal tax = sub * (prod.TaxRate / 100m);
//                     runningSubtotal += sub;
//                     runningTax += tax;

//                     pendingItems.Add(new PendingOrderItem
//                     {
//                         Product = prod,
//                         Request = itemReq,
//                         ItemSubtotal = sub,
//                         ItemTax = tax,
//                         ItemTotalBeforeDiscount = sub + tax
//                     });
//                 }
//             }

//             decimal runningTotalWithTax = runningSubtotal + runningTax;

//             // ==================== Discount ====================
//             var allActiveDiscounts = await _context.Discounts
//                 .Include(d => d.ProductDiscounts)
//                 .Where(d => !d.IsDeleted && d.IsActive
//                     && (d.StartDate == null || d.StartDate <= now)
//                     && (d.EndDate == null || d.EndDate >= now))
//                 .ToListAsync(cancellationToken);

//             decimal totalAutoDiscount = 0;

//             var applicableDiscounts = allActiveDiscounts
//                 .Where(d => d.MinOrderAmount == null || runningTotalWithTax >= d.MinOrderAmount)
//                 .ToList();

//             foreach (var d in applicableDiscounts)
//             {
//                 var specificProductIds = d.ProductDiscounts
//                     .Where(pd => !pd.IsDeleted)
//                     .Select(pd => pd.ProductId)
//                     .ToList();

//                 if (specificProductIds.Any())
//                 {
//                     decimal eligible = pendingItems
//                         .Where(p => specificProductIds.Contains(p.Product.Id))
//                         .Sum(p => p.ItemTotalBeforeDiscount);

//                     totalAutoDiscount += d.Type == "Percentage"
//                         ? eligible * (d.Value / 100m)
//                         : d.Value;
//                 }
//                 else
//                 {
//                     totalAutoDiscount += d.Type == "Percentage"
//                         ? runningTotalWithTax * (d.Value / 100m)
//                         : d.Value;
//                 }
//             }

//             decimal finalDiscountTotal = Math.Min(
//                 totalAutoDiscount + (request.DiscountAmount ?? 0),
//                 runningTotalWithTax);

//             decimal discountRate = runningTotalWithTax > 0 ? finalDiscountTotal / runningTotalWithTax : 0;
//             decimal finalTotal = runningTotalWithTax - finalDiscountTotal;

//             // ==================== Order Items ====================
//             var orderItems = new List<DomainOrderItem>();

//             foreach (var pending in pendingItems)
//             {
//                 decimal itemDiscount = Math.Round(pending.ItemTotalBeforeDiscount * discountRate, 2);

//                 orderItems.Add(new DomainOrderItem
//                 {
//                     ProductId = pending.Product.Id,
//                     ImageProduct = pending.Product.ImageProduct,
//                     SerialNumberId = pending.Serial?.Id,
//                     Quantity = pending.Serial != null ? 1 : pending.Request.Quantity,
//                     UnitPrice = pending.Request.UnitPrice,
//                     SubTotal = pending.ItemSubtotal,
//                     DiscountAmount = itemDiscount,
//                     WarrantyMonths = pending.Request.WarrantyMonths,
//                     WarrantyStartDate = pending.Request.WarrantyMonths > 0 ? now : null,
//                     WarrantyEndDate = pending.Request.WarrantyMonths > 0
//                         ? now.AddMonths(pending.Request.WarrantyMonths.Value) : null
//                 });
//             }

//             if (orderItems.Any())
//             {
//                 var diff = finalDiscountTotal - orderItems.Sum(i => i.DiscountAmount);
//                 orderItems.Last().DiscountAmount += diff;
//             }

//             // // ==================== Calculate EarnedPoints ====================
//             // int earnedPoints = 0;
//             // if (request.PaymentMethod != PaymentMethodCode.Point)
//             // {
//             //     var pointSetup = await _context.PointSetups
//             //         .AsNoTracking()
//             //         .FirstOrDefaultAsync(p => p.Id == 1, cancellationToken);

//             //     if (pointSetup != null && pointSetup.IsActive)
//             //     {
//             //         if (finalTotal >= pointSetup.MinOrderAmount)
//             //         {
//             //             decimal raw = finalTotal * pointSetup.PointValue;
//             //             earnedPoints = (int)Math.Floor(raw);

//             //             // Cap if MaxPointPerOrder set
//             //             if (pointSetup.MaxPointPerOrder.HasValue)
//             //                 earnedPoints = Math.Min(earnedPoints, pointSetup.MaxPointPerOrder.Value);
//             //         }
//             //     }
//             // }
//             // ==================== Calculate EarnedPoints ====================
//             int earnedPoints = 0;
//             int pointsToDeduct = 0;

//             var pointSetup = await _context.PointSetups
//                 .AsNoTracking()
//                 .FirstOrDefaultAsync(p => p.Id == 1, cancellationToken);

//             if (request.PaymentMethod == PaymentMethodCode.Point)
//             {
//                 earnedPoints = 0;
//                 pointsToDeduct = request.PointsUsed;
//             }
//             else
//             {
//                 if (pointSetup != null && pointSetup.IsActive && finalTotal >= pointSetup.MinOrderAmount)
//                 {
//                     decimal raw = finalTotal * pointSetup.PointValue;
//                     earnedPoints = (int)Math.Floor(raw);
//                     if (pointSetup.MaxPointPerOrder.HasValue)
//                         earnedPoints = Math.Min(earnedPoints, pointSetup.MaxPointPerOrder.Value);
//                 }
//             }

//             // ==================== Create Order ====================
//             var order = new DomainOrder
//             {
//                 OrderNumber = $"ORD-{now:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
//                 OrderDate = now,
//                 CustomerId = request.CustomerId,
//                 StaffId = _currentUser.UserId,
//                 SubTotal = runningSubtotal,
//                 TaxAmount = runningTax,
//                 DiscountAmount = finalDiscountTotal,
//                 TotalAmount = finalTotal,
//                 Status = request.Status ?? 0,
//                 SaleType = request.SaleType ?? 0,
//                 PaymentStatus = request.PaymentStatus ?? 0,
//                 PaymentMethod = request.PaymentMethod,
//                 OrderItems = orderItems,
//                 Notes = request.Notes,
//                 EarnedPoints = earnedPoints,
//             };

//             _context.Orders.Add(order);

//             // ==================== Update Customer TotalPoint ====================
//             // if (request.CustomerId.HasValue && earnedPoints > 0)
//             // {
//             //     var customer = await _context.Customers
//             //         .FirstOrDefaultAsync(c => c.Id == request.CustomerId.Value && !c.IsDeleted, cancellationToken);

//             //     if (customer != null)
//             //         customer.TotalPoint += earnedPoints;
//             // }

//             // await _context.SaveChangesAsync(cancellationToken);
//             if (request.CustomerId.HasValue)
//             {
//                 var customer = await _context.Customers
//                     .FirstOrDefaultAsync(c => c.Id == request.CustomerId.Value && !c.IsDeleted, cancellationToken);

//                 if (customer != null)
//                 {
//                     if (earnedPoints > 0)
//                         customer.TotalPoint += earnedPoints;

//                     if (pointsToDeduct > 0)
//                         customer.TotalPoint = Math.Max(0, customer.TotalPoint - pointsToDeduct); // ✅ deduct point
//                 }
//             }

//             await _context.SaveChangesAsync(cancellationToken);

//             return ApiResponse<OrderCreateResponse>.Created(
//                 await GetOrderInfo(order.Id, cancellationToken),
//                 "Order created successfully");
//         }

//         // ==================== GetOrderInfo ====================
//         private async Task<OrderCreateResponse> GetOrderInfo(int orderId, CancellationToken cancellationToken)
//         {
//             var o = await _context.Orders
//                 .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
//                 .Include(o => o.OrderItems).ThenInclude(oi => oi.SerialNumber)
//                 .AsNoTracking()
//                 .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

//             var staff = o!.StaffId.HasValue
//                 ? await _context.Persons
//                     .AsNoTracking()
//                     .Where(p => p.Id == o.StaffId)
//                     .Select(p => new TypeNamebase { Id = p.Id, Name = p.Username ?? "N/A" })
//                     .FirstOrDefaultAsync(cancellationToken)
//                 : null;

//             var customer = o.CustomerId.HasValue
//                 ? await _context.Customers
//                     .AsNoTracking()
//                     .Where(c => c.Id == o.CustomerId)
//                     .Select(c => new TypeNamebase
//                     {
//                         Id = c.Id,
//                         Name = c.FirstName + " " + c.LastName
//                     })
//                     .FirstOrDefaultAsync(cancellationToken)
//                 : null;

//             return new OrderCreateResponse
//             {
//                 Id = o.Id,
//                 OrderNumber = o.OrderNumber,
//                 OrderDate = o.OrderDate,
//                 CustomerId = o.CustomerId,
//                 Customer = customer,
//                 StaffId = o.StaffId,
//                 Staff = staff,
//                 SubTotal = o.SubTotal,
//                 TaxAmount = o.TaxAmount,
//                 TotalAmount = o.TotalAmount,
//                 DiscountAmount = o.DiscountAmount,
//                 EarnedPoints = o.EarnedPoints,
//                 PaymentMethod = new TypeNamebase
//                 {
//                     Id = (int)(o.PaymentMethod ?? 0),
//                     Name = o.PaymentMethod.ToString() ?? "N/A"
//                 },
//                 PaymentStatus = new TypeNamebase
//                 {
//                     Id = (int)o.PaymentStatus,
//                     Name = o.PaymentStatus.ToString()
//                 },
//                 SaleType = new TypeNamebase
//                 {
//                     Id = (int)o.SaleType,
//                     Name = o.SaleType.ToString()
//                 },
//                 Status = new TypeNamebase
//                 {
//                     Id = (int)o.Status,
//                     Name = o.Status.ToString()
//                 },
//                 Notes = o.Notes,
//                 OrderItems = o.OrderItems.Select(oi => new OrderItemInfo
//                 {
//                     Id = oi.Id,
//                     OrderId = oi.OrderId,
//                     ProductId = oi.ProductId,
//                     ImageProduct = oi.ImageProduct ?? "",
//                     ProductName = oi.Product.Name,
//                     Quantity = oi.Quantity,
//                     UnitPrice = oi.UnitPrice,
//                     SubTotal = oi.SubTotal,
//                     SerialNumberId = oi.SerialNumberId,
//                     SerialNo = oi.SerialNumber != null ? new TypeNamebase
//                     {
//                         Id = (int)(oi.SerialNumberId ?? 0),
//                         Name = oi.SerialNumber.SerialNo ?? "N/A"
//                     } : null,
//                     WarrantyMonths = oi.WarrantyMonths,
//                     WarrantyStartDate = oi.WarrantyStartDate,
//                     WarrantyEndDate = oi.WarrantyEndDate
//                 }).ToList()
//             };
//         }
//     }
// }










using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using POS.Domain.Enums;

using DomainOrder = POS.Domain.Entities.Order;
using DomainOrderItem = POS.Domain.Entities.OrderItem;
using DomainProduct = POS.Domain.Entities.Product;
using DomainSerialNumber = POS.Domain.Entities.SerialNumber;

namespace POS.Application.Features.Order
{
    public class OrderItemCreateDto
    {
        public int ProductId { get; set; }
        public List<int>? SerialNumberIds { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Notes { get; set; }
        public int? WarrantyMonths { get; set; }
    }

    public record OrderCreateCommand : IRequest<ApiResponse<OrderCreateResponse>>
    {
        public int? CustomerId { get; set; }
        public decimal? DiscountAmount { get; set; }
        public SaleType? SaleType { get; set; }
        public OrderStatus? Status { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public PaymentMethodCode? PaymentMethod { get; set; }
        public string? Notes { get; set; }
        public int PointsUsed { get; set; } = 0;
        public List<OrderItemCreateDto> Items { get; set; } = new();
    }

    internal sealed class PendingOrderItem
    {
        public DomainProduct Product { get; set; } = null!;
        public OrderItemCreateDto Request { get; set; } = null!;
        public DomainSerialNumber? Serial { get; set; }
        public decimal ItemSubtotal { get; set; }
        public decimal ItemTax { get; set; }
        public decimal ItemTotalBeforeDiscount { get; set; }
    }

    public class OrderCreateCommandHandler : IRequestHandler<OrderCreateCommand, ApiResponse<OrderCreateResponse>>
    {
        private readonly IMyAppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public OrderCreateCommandHandler(IMyAppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<ApiResponse<OrderCreateResponse>> Handle(
            OrderCreateCommand request,
            CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var pendingItems = new List<PendingOrderItem>();
            decimal runningSubtotal = 0;
            decimal runningTax = 0;

            // ==================== Build Items ====================
            foreach (var itemReq in request.Items)
            {
                var prod = await _context.Products
                    .Include(p => p.SerialNumbers.Where(s => !s.IsDeleted))
                    .Include(p => p.ProductDiscounts.Where(pd => !pd.IsDeleted))
                    .FirstOrDefaultAsync(p => p.Id == itemReq.ProductId && !p.IsDeleted, cancellationToken);

                if (prod == null)
                    return ApiResponse<OrderCreateResponse>.NotFound($"Product {itemReq.ProductId} not found");

                if (prod.IsSerialNumber)
                {
                    foreach (var sId in itemReq.SerialNumberIds?.Distinct() ?? new List<int>())
                    {
                        var serial = prod.SerialNumbers.FirstOrDefault(s => s.Id == sId);
                        if (serial == null || serial.Status != "Available") continue;

                        serial.Status = "Sold";
                        serial.SoldDate = now;
                        prod.Stock -= 1;

                        decimal sub = itemReq.UnitPrice;
                        decimal tax = sub * (prod.TaxRate / 100m);
                        runningSubtotal += sub;
                        runningTax += tax;

                        pendingItems.Add(new PendingOrderItem
                        {
                            Product = prod,
                            Request = itemReq,
                            Serial = serial,
                            ItemSubtotal = sub,
                            ItemTax = tax,
                            ItemTotalBeforeDiscount = sub + tax
                        });
                    }
                }
                else
                {
                    if (prod.Stock < itemReq.Quantity)
                        return ApiResponse<OrderCreateResponse>.BadRequest($"Stock low for {prod.Name}");

                    prod.Stock -= itemReq.Quantity;

                    decimal sub = itemReq.Quantity * itemReq.UnitPrice;
                    decimal tax = sub * (prod.TaxRate / 100m);
                    runningSubtotal += sub;
                    runningTax += tax;

                    pendingItems.Add(new PendingOrderItem
                    {
                        Product = prod,
                        Request = itemReq,
                        ItemSubtotal = sub,
                        ItemTax = tax,
                        ItemTotalBeforeDiscount = sub + tax
                    });
                }
            }

            decimal runningTotalWithTax = runningSubtotal + runningTax;

            // ==================== Discount ====================
            var allActiveDiscounts = await _context.Discounts
                .Include(d => d.ProductDiscounts)
                .Where(d => !d.IsDeleted && d.IsActive
                    && (d.StartDate == null || d.StartDate <= now)
                    && (d.EndDate == null || d.EndDate >= now))
                .ToListAsync(cancellationToken);

            decimal totalAutoDiscount = 0;

            var applicableDiscounts = allActiveDiscounts
                .Where(d => d.MinOrderAmount == null || runningTotalWithTax >= d.MinOrderAmount)
                .ToList();

            foreach (var d in applicableDiscounts)
            {
                var specificProductIds = d.ProductDiscounts
                    .Where(pd => !pd.IsDeleted)
                    .Select(pd => pd.ProductId)
                    .ToList();

                if (specificProductIds.Any())
                {
                    decimal eligible = pendingItems
                        .Where(p => specificProductIds.Contains(p.Product.Id))
                        .Sum(p => p.ItemTotalBeforeDiscount);

                    totalAutoDiscount += d.Type == "Percentage"
                        ? eligible * (d.Value / 100m)
                        : d.Value;
                }
                else
                {
                    totalAutoDiscount += d.Type == "Percentage"
                        ? runningTotalWithTax * (d.Value / 100m)
                        : d.Value;
                }
            }

            decimal finalDiscountTotal = Math.Min(
                totalAutoDiscount + (request.DiscountAmount ?? 0),
                runningTotalWithTax);

            decimal discountRate = runningTotalWithTax > 0 ? finalDiscountTotal / runningTotalWithTax : 0;
            decimal finalTotal = runningTotalWithTax - finalDiscountTotal;

            // ==================== Order Items ====================
            var orderItems = new List<DomainOrderItem>();

            foreach (var pending in pendingItems)
            {
                decimal itemDiscount = Math.Round(pending.ItemTotalBeforeDiscount * discountRate, 2);

                orderItems.Add(new DomainOrderItem
                {
                    ProductId = pending.Product.Id,
                    ImageProduct = pending.Product.ImageProduct,
                    SerialNumberId = pending.Serial?.Id,
                    Quantity = pending.Serial != null ? 1 : pending.Request.Quantity,
                    UnitPrice = pending.Request.UnitPrice,
                    SubTotal = pending.ItemSubtotal,
                    DiscountAmount = itemDiscount,
                    WarrantyMonths = pending.Request.WarrantyMonths,
                    WarrantyStartDate = pending.Request.WarrantyMonths > 0 ? now : null,
                    WarrantyEndDate = pending.Request.WarrantyMonths > 0
                        ? now.AddMonths(pending.Request.WarrantyMonths.Value) : null
                });
            }

            if (orderItems.Any())
            {
                var diff = finalDiscountTotal - orderItems.Sum(i => i.DiscountAmount);
                orderItems.Last().DiscountAmount += diff;
            }

            // ==================== Point Setup ====================
            var pointSetup = await _context.PointSetups
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == 1, cancellationToken);

            // ==================== Calculate EarnedPoints + CashReceived ====================
            int earnedPoints = 0;
            int pointsToDeduct = 0;
            decimal cashReceived = 0;

            if (request.PaymentMethod == PaymentMethodCode.Point)
            {
                // ✅ Pay by Point — no cash received, no earn, only deduct
                earnedPoints = 0;
                pointsToDeduct = request.PointsUsed;
                cashReceived = 0;
            }
            else
            {
                // ✅ Cash / QR — actual cash received, calculate earned points
                cashReceived = finalTotal;

                if (pointSetup != null && pointSetup.IsActive && finalTotal >= pointSetup.MinOrderAmount)
                {
                    decimal raw = finalTotal * pointSetup.PointValue;
                    earnedPoints = (int)Math.Floor(raw);

                    if (pointSetup.MaxPointPerOrder.HasValue)
                        earnedPoints = Math.Min(earnedPoints, pointSetup.MaxPointPerOrder.Value);
                }
            }

            // ==================== Create Order ====================
            var order = new DomainOrder
            {
                OrderNumber = $"ORD-{now:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                OrderDate = now,
                CustomerId = request.CustomerId,
                StaffId = _currentUser.UserId,
                SubTotal = runningSubtotal,
                TaxAmount = runningTax,
                DiscountAmount = finalDiscountTotal,
                TotalAmount = finalTotal,
                Status = request.Status ?? 0,
                SaleType = request.SaleType ?? 0,
                PaymentStatus = request.PaymentStatus ?? 0,
                PaymentMethod = request.PaymentMethod,
                OrderItems = orderItems,
                Notes = request.Notes,
                EarnedPoints = earnedPoints,
                PointsUsed = pointsToDeduct,  // ✅
                CashReceived = cashReceived,     // ✅
            };

            _context.Orders.Add(order);

            // ==================== Update Customer TotalPoint ====================
            if (request.CustomerId.HasValue)
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == request.CustomerId.Value && !c.IsDeleted, cancellationToken);

                if (customer != null)
                {
                    if (earnedPoints > 0)
                        customer.TotalPoint += earnedPoints;                               // ✅ earn

                    if (pointsToDeduct > 0)
                        customer.TotalPoint = Math.Max(0, customer.TotalPoint - pointsToDeduct); // ✅ deduct
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<OrderCreateResponse>.Created(
                await GetOrderInfo(order.Id, cancellationToken),
                "Order created successfully");
        }

        // ==================== GetOrderInfo ====================
        private async Task<OrderCreateResponse> GetOrderInfo(int orderId, CancellationToken cancellationToken)
        {
            var o = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.SerialNumber)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            var staff = o!.StaffId.HasValue
                ? await _context.Persons
                    .AsNoTracking()
                    .Where(p => p.Id == o.StaffId)
                    .Select(p => new TypeNamebase { Id = p.Id, Name = p.Username ?? "N/A" })
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            var customer = o.CustomerId.HasValue
                ? await _context.Customers
                    .AsNoTracking()
                    .Where(c => c.Id == o.CustomerId)
                    .Select(c => new TypeNamebase
                    {
                        Id = c.Id,
                        Name = c.FirstName + " " + c.LastName
                    })
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            return new OrderCreateResponse
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                CustomerId = o.CustomerId,
                Customer = customer,
                StaffId = o.StaffId,
                Staff = staff,
                SubTotal = o.SubTotal,
                TaxAmount = o.TaxAmount,
                TotalAmount = o.TotalAmount,
                DiscountAmount = o.DiscountAmount,
                EarnedPoints = o.EarnedPoints,
                PointsUsed = o.PointsUsed,     // ✅
                CashReceived = o.CashReceived,   // ✅
                PaymentMethod = new TypeNamebase
                {
                    Id = (int)(o.PaymentMethod ?? 0),
                    Name = o.PaymentMethod.ToString() ?? "N/A"
                },
                PaymentStatus = new TypeNamebase
                {
                    Id = (int)o.PaymentStatus,
                    Name = o.PaymentStatus.ToString()
                },
                SaleType = new TypeNamebase
                {
                    Id = (int)o.SaleType,
                    Name = o.SaleType.ToString()
                },
                Status = new TypeNamebase
                {
                    Id = (int)o.Status,
                    Name = o.Status.ToString()
                },
                Notes = o.Notes,
                OrderItems = o.OrderItems.Select(oi => new OrderItemInfo
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    ProductId = oi.ProductId,
                    ImageProduct = oi.ImageProduct ?? "",
                    ProductName = oi.Product.Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    SubTotal = oi.SubTotal,
                    SerialNumberId = oi.SerialNumberId,
                    SerialNo = oi.SerialNumber != null ? new TypeNamebase
                    {
                        Id = (int)(oi.SerialNumberId ?? 0),
                        Name = oi.SerialNumber.SerialNo ?? "N/A"
                    } : null,
                    WarrantyMonths = oi.WarrantyMonths,
                    WarrantyStartDate = oi.WarrantyStartDate,
                    WarrantyEndDate = oi.WarrantyEndDate
                }).ToList()
            };
        }
    }
}