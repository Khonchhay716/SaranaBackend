using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;

namespace POS.Application.Common.Interfaces
{
    public interface IMyAppDbContext
    {
        DbSet<Product> Products { get; }
        DbSet<Person> Persons { get; }
        DbSet<Coupon> Coupons { get; }
        DbSet<Book> Books { get; }
        DbSet<Permission> Permissions { get; }
        DbSet<RolePermission> RolePermissions { get; }
        DbSet<PersonRole> PersonRoles { get; }
        DbSet<Role> Roles { get; }
        DbSet<RefreshToken> RefreshTokens { get; }
        DbSet<LibraryMember> LibraryMembers { get; }
        DbSet<BookIssue> BookIssues { get; }
        DbSet<Category> Categories { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
        int SaveChanges();
    }

}