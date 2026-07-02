using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Domain.Entities.StockManagement;

namespace POS.Application.Common.Interfaces
{
    public interface IMyAppDbContext
    {
        DbSet<Person> Persons { get; }
        DbSet<Staff> Staffs { get; }
        DbSet<Customer> Customers { get; }
        DbSet<Permission> Permissions { get; }
        DbSet<RolePermission> RolePermissions { get; }
        DbSet<PersonRole> PersonRoles { get; }
        DbSet<Role> Roles { get; }
        DbSet<RefreshToken> RefreshTokens { get; }

        DbSet<Category> Categories { get; }
        DbSet<Discount> Discounts { get; }
        DbSet<ProductDiscount> ProductDiscounts { get; }
        DbSet<LeaveType> LeaveTypes { get; }
        DbSet<LeaveRequest> LeaveRequests { get; }
        DbSet<LeaveBalance> LeaveBalances { get; }
        DbSet<PointSetup> PointSetups { get; }


        // Stock Management
        DbSet<Supplier> Suppliers { get; set; }
        DbSet<Product> Products { get; set; }
        DbSet<SerialStock> SerialStocks { get; set; }
        DbSet<NonSerialStock> NonSerialStocks { get; set; }
        DbSet<StockMovement> StockMovements { get; set; }
        DbSet<StockAdjustment> StockAdjustments { get; set; }
        DbSet<StockReturn> StockReturns { get; set; }
        DbSet<StockReturnItem> StockReturnItems { get; set; }

        DbSet<Order> Orders { get; set;}
        DbSet<OrderItem> OrderItems { get; set;}

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
        int SaveChanges();
    }

}