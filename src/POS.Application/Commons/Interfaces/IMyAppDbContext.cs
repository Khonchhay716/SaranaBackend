using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;

namespace POS.Application.Common.Interfaces
{
    public interface IMyAppDbContext
    {
        DbSet<Person> Persons { get; }
        DbSet<Permission> Permissions { get; }
        DbSet<RolePermission> RolePermissions { get; }
        DbSet<PersonRole> PersonRoles { get; }
        DbSet<Role> Roles { get; }
        DbSet<RefreshToken> RefreshTokens { get; }

        DbSet<Category> Categories { get; }
        DbSet<Branch> Branches { get; }
        DbSet<Product> Products { get; }
        DbSet<SerialNumber> SerialNumbers { get; }
        DbSet<StockMovement> StockMovements { get; }
        DbSet<Order> Orders { get; }
        DbSet<Discount> Discounts { get; }
        DbSet<ProductDiscount> ProductDiscounts { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
        int SaveChanges();
    }

}