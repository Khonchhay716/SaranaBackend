using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Interfaces;
using POS.Application.Features.Permission;
using POS.Domain.Entities;
using POS.Infrastructure.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace POS.Infrastructure.Data
{
    public class DatabaseSeeder
    {
        private readonly MyAppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public DatabaseSeeder(MyAppDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task SeedAsync()
        {
            try
            {
                await _context.Database.MigrateAsync();
                await SeedSuperAdminRoleAsync();
                await SeedSuperAdminUserAsync();
                await SeedAllPermissionsToSuperAdminAsync();

                await _context.SaveChangesAsync();
                Console.WriteLine("Database Seeding Completed Successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Seeding Error:");
                Console.WriteLine($"{ex.Message}");
                throw;
            }
        }

        // Seeding SuperAdmin Role
        private async Task SeedSuperAdminRoleAsync()
        {
            var superAdminRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "SuperAdmin" && !r.IsDeleted);

            if (superAdminRole == null)
            {
                superAdminRole = new Role
                {
                    Name = "SuperAdmin",
                    Description = "Super Administrator with full system access and all permissions",
                    IsDeleted = false,
                    CreatedDate = DateTimeOffset.UtcNow,
                    UpdatedDate = DateTimeOffset.UtcNow
                };

                _context.Roles.Add(superAdminRole);
                await _context.SaveChangesAsync();

                Console.WriteLine("✓ SuperAdmin role created.");
            }
            else
            {
                Console.WriteLine("✓ SuperAdmin role already exists.");
            }
        }

        // function for create user seeding 
        private async Task SeedSuperAdminUserAsync()
        {
            var superAdminUser = await _context.Persons
                .FirstOrDefaultAsync(p => p.Username == "superadmin");

            if (superAdminUser == null)
            {
                var defaultPassword = Environment.GetEnvironmentVariable("SUPERADMIN_DEFAULT_PASSWORD") ?? "Password123!";
                string hashedPassword = _passwordHasher.HashPassword(defaultPassword);
                superAdminUser = new Person
                {
                    Username = "superadmin",
                    Email = "superadmin@system.com",
                    PasswordHash = hashedPassword,
                    IsActive = true,
                    IsDeleted = false,
                    StaffId = null, 
                    CustomerId = null,
                    CreatedDate = DateTimeOffset.UtcNow,
                    UpdatedDate = DateTimeOffset.UtcNow
                };

                _context.Persons.Add(superAdminUser);
                await _context.SaveChangesAsync();

                // Assign SuperAdmin Role
                var superAdminRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == "SuperAdmin" && !r.IsDeleted);

                if (superAdminRole != null)
                {
                    _context.PersonRoles.Add(new PersonRole
                    {
                        PersonId = superAdminUser.Id,
                        RoleId = superAdminRole.Id
                    });
                    await _context.SaveChangesAsync();
                }
                Console.WriteLine("✓ SuperAdmin user created (StaffId=null, CustomerId=null).");
            }
            else
            {
                Console.WriteLine("✓ SuperAdmin user already exists.");
            }
        }

        // assign permission to role 
        private async Task SeedAllPermissionsToSuperAdminAsync()
        {
            var superAdminRole = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Name == "SuperAdmin" && !r.IsDeleted);

            if (superAdminRole == null) return;

            var allPermissions = PermissionData.Permissions.Select(p => p.Name).ToList();
            var existingPermissions = superAdminRole.RolePermissions
                .Select(rp => rp.PermissionName)
                .ToHashSet();

            var permissionsToAdd = allPermissions
                .Where(p => !existingPermissions.Contains(p))
                .ToList();

            if (permissionsToAdd.Any())
            {
                foreach (var permissionName in permissionsToAdd)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = superAdminRole.Id,
                        PermissionName = permissionName
                    });
                }
                await _context.SaveChangesAsync();

                Console.WriteLine($"{permissionsToAdd.Count} permissions added to SuperAdmin role.");
            }
            else
            {
                Console.WriteLine("SuperAdmin role already has all permissions.");
            }
        }
    }
}