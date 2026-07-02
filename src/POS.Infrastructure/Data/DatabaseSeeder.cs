using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using POS.Application.Common.Interfaces;
using POS.Application.Features.Permission;
using POS.Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Infrastructure.Data
{
    public class DatabaseSeeder : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly MyAppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public DatabaseSeeder(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;

            var provider = serviceProvider.CreateScope().ServiceProvider;
            _context = provider.GetRequiredService<MyAppDbContext>();
            _passwordHasher = provider.GetRequiredService<IPasswordHasher>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine(" Starting Database Seeding via Hosted Service...");

                // Update Database (Migration) auto
                // await _context.Database.MigrateAsync(cancellationToken);
                await SeedSuperAdminRoleAsync();
                await SeedUserRoleAsync();
                await SeedSuperAdminUserAsync();
                await SeedAllPermissionsToSuperAdminAsync();
                await _context.SaveChangesAsync(cancellationToken);

                Console.WriteLine("Database Seeding Completed Successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Seeding Error occurred:");
                Console.WriteLine($"   {ex.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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

                Console.WriteLine("SuperAdmin role created.");
            }
            else
            {
                Console.WriteLine("SuperAdmin role already exists.");
            }
        }

        private async Task SeedUserRoleAsync()
        {
            var userRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "User Default" && !r.IsDeleted);

            if (userRole == null)
            {
                _context.Roles.Add(new Role
                {
                    Name = "User Default",
                    Description = "User Role that create with yourself",
                    IsDeleted = false,
                    CreatedDate = DateTimeOffset.UtcNow,
                    UpdatedDate = DateTimeOffset.UtcNow
                });
                await _context.SaveChangesAsync();
                Console.WriteLine("User Default role created.");
            }
            else
            {
                Console.WriteLine("User Default role already exists.");
            }
        }

        private async Task SeedSuperAdminUserAsync()
        {
            var superAdminUser = await _context.Persons
                .FirstOrDefaultAsync(p => p.Username == "superadmin");

            if (superAdminUser == null)
            {
                var defaultPassword = _configuration["SuperAdmin:DefaultPassword"] ?? "Password123!";
                string hashedPassword = _passwordHasher.HashPassword(defaultPassword);

                superAdminUser = new Person
                {
                    Username = "superadmin",
                    Email = "superadmin@system.com",
                    PasswordHash = hashedPassword,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedDate = DateTimeOffset.UtcNow,
                    UpdatedDate = DateTimeOffset.UtcNow
                };

                _context.Persons.Add(superAdminUser);
                await _context.SaveChangesAsync();

                // Assign SuperAdmin Role ឱ្យទៅ User នេះ
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
                Console.WriteLine("SuperAdmin user created (StaffId=null, CustomerId=null).");
            }
            else
            {
                Console.WriteLine("SuperAdmin user already exists.");
            }
        }

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