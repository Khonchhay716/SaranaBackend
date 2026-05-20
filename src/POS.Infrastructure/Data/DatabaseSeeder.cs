// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Configuration;
// using POS.Application.Common.Interfaces;
// using POS.Application.Features.Permission;
// using POS.Domain.Entities;
// using POS.Infrastructure.Services;
// using System;
// using System.Linq;
// using System.Threading.Tasks;

// namespace POS.Infrastructure.Data
// {
//     public class DatabaseSeeder
//     {
//         private readonly MyAppDbContext _context;
//         private readonly IPasswordHasher _passwordHasher;
//         private readonly IConfiguration _configuration;

//         public DatabaseSeeder(MyAppDbContext context, IPasswordHasher passwordHasher, IConfiguration configuration)
//         {
//             _context = context;
//             _passwordHasher = passwordHasher;
//             _configuration = configuration;
//         }

//         public async Task SeedAsync()
//         {
//             try
//             {
//                 await _context.Database.MigrateAsync();
//                 await SeedSuperAdminRoleAsync();
//                 await SeedUserRoleAsync();
//                 await SeedSuperAdminUserAsync();
//                 await SeedAllPermissionsToSuperAdminAsync();

//                 await _context.SaveChangesAsync();
//                 Console.WriteLine("Database Seeding Completed Successfully!");
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine("Seeding Error:");
//                 Console.WriteLine($"{ex.Message}");
//                 throw;
//             }
//         }

//         // function this work for check in table role have role SuperAdmin or not ? if have no create and if no is create auto 
//         private async Task SeedSuperAdminRoleAsync()
//         {
//             var superAdminRole = await _context.Roles
//                 .FirstOrDefaultAsync(r => r.Name == "SuperAdmin" && !r.IsDeleted);

//             if (superAdminRole == null)
//             {
//                 superAdminRole = new Role
//                 {
//                     Name = "SuperAdmin",
//                     Description = "Super Administrator with full system access and all permissions",
//                     IsDeleted = false,
//                     CreatedDate = DateTimeOffset.UtcNow,
//                     UpdatedDate = DateTimeOffset.UtcNow
//                 };

//                 _context.Roles.Add(superAdminRole);
//                 await _context.SaveChangesAsync();

//                 Console.WriteLine("✓ SuperAdmin role created.");
//             }
//             else
//             {
//                 Console.WriteLine("✓ SuperAdmin role already exists.");
//             }
//         }

//         // function this work for check in table role have role User Default or not ? if have no create and if no is create auto 
//         private async Task SeedUserRoleAsync()
//         {
//             var userRole = await _context.Roles
//                 .FirstOrDefaultAsync(r => r.Name == "User Default" && !r.IsDeleted);

//             if (userRole == null)
//             {
//                 _context.Roles.Add(new Role
//                 {
//                     Name = "User Default",
//                     Description = "User Role that create with yourself",
//                     IsDeleted = false,
//                     CreatedDate = DateTimeOffset.UtcNow,
//                     UpdatedDate = DateTimeOffset.UtcNow
//                 });
//                 await _context.SaveChangesAsync();
//                 Console.WriteLine(" User Default role created.");
//             }
//             else
//             {
//                 Console.WriteLine(" User Default role already exists.");
//             }
//         }

//         // function for create user seeding 
//         private async Task SeedSuperAdminUserAsync()
//         {
//             var superAdminUser = await _context.Persons
//                 .FirstOrDefaultAsync(p => p.Username == "superadmin");

//             if (superAdminUser == null)
//             {
//                 var defaultPassword = _configuration["SuperAdmin:DefaultPassword"] ?? "Password123!";
//                 string hashedPassword = _passwordHasher.HashPassword(defaultPassword);
//                 superAdminUser = new Person
//                 {
//                     Username = "superadmin",
//                     Email = "superadmin@system.com",
//                     PasswordHash = hashedPassword,
//                     IsActive = true,
//                     IsDeleted = false,
//                     StaffId = null,
//                     CustomerId = null,
//                     CreatedDate = DateTimeOffset.UtcNow,
//                     UpdatedDate = DateTimeOffset.UtcNow
//                 };

//                 _context.Persons.Add(superAdminUser);
//                 await _context.SaveChangesAsync();

//                 // Assign SuperAdmin Role
//                 var superAdminRole = await _context.Roles
//                     .FirstOrDefaultAsync(r => r.Name == "SuperAdmin" && !r.IsDeleted);

//                 if (superAdminRole != null)
//                 {
//                     _context.PersonRoles.Add(new PersonRole
//                     {
//                         PersonId = superAdminUser.Id,
//                         RoleId = superAdminRole.Id
//                     });
//                     await _context.SaveChangesAsync();
//                 }
//                 Console.WriteLine("SuperAdmin user created (StaffId=null, CustomerId=null).");
//             }
//             else
//             {
//                 Console.WriteLine("SuperAdmin user already exists.");
//             }
//         }

//         // assign permission to role 
//         private async Task SeedAllPermissionsToSuperAdminAsync()
//         {
//             var superAdminRole = await _context.Roles
//                 .Include(r => r.RolePermissions)
//                 .FirstOrDefaultAsync(r => r.Name == "SuperAdmin" && !r.IsDeleted);

//             if (superAdminRole == null) return;

//             var allPermissions = PermissionData.Permissions.Select(p => p.Name).ToList();
//             var existingPermissions = superAdminRole.RolePermissions
//                 .Select(rp => rp.PermissionName)
//                 .ToHashSet();

//             var permissionsToAdd = allPermissions
//                 .Where(p => !existingPermissions.Contains(p))
//                 .ToList();

//             if (permissionsToAdd.Any())
//             {
//                 foreach (var permissionName in permissionsToAdd)
//                 {
//                     _context.RolePermissions.Add(new RolePermission
//                     {
//                         RoleId = superAdminRole.Id,
//                         PermissionName = permissionName
//                     });
//                 }
//                 await _context.SaveChangesAsync();

//                 Console.WriteLine($"{permissionsToAdd.Count} permissions added to SuperAdmin role.");
//             }
//             else
//             {
//                 Console.WriteLine("SuperAdmin role already has all permissions.");
//             }
//         }
//     }
// }



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
    // 🔥 អនុវត្តពី IHostedService ដើម្បីឱ្យវាក្លាយជា Background Service រត់ស្វ័យប្រវត្តិ
    public class DatabaseSeeder : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly MyAppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        // 🎯 ទទួលយក IServiceProvider ដើម្បីបង្កើត Scope ទាញយកសេវាកម្ម Scoped ដូចមេរបស់ប្រូ
        public DatabaseSeeder(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;

            // 🧠 បង្កើត Scope សិប្បនិម្មិតដើម្បីទាញយក DbContext និង PasswordHasher មកប្រើប្រាស់
            var provider = serviceProvider.CreateScope().ServiceProvider;
            _context = provider.GetRequiredService<MyAppDbContext>();
            _passwordHasher = provider.GetRequiredService<IPasswordHasher>();
        }

        // 🚀 មុខងារមេដែល .NET នឹងហៅឱ្យរត់ភ្លាមពេល Server ចាប់ផ្ដើមបើកដំណើរការ
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("⏳ Starting Database Seeding via Hosted Service...");

                // ១. រត់កូដ Update Database (Migration) ស្វ័យប្រវត្តិ
                await _context.Database.MigrateAsync(cancellationToken);

                // ២. បញ្ឆេះមុខងារលម្អិតដើម្បីចាក់ទិន្នន័យដំបូង
                await SeedSuperAdminRoleAsync();
                await SeedUserRoleAsync();
                await SeedSuperAdminUserAsync();
                await SeedAllPermissionsToSuperAdminAsync();

                // ៣. រក្សាទុកការផ្លាស់ប្តូរទាំងអស់ចូល PostgreSQL
                await _context.SaveChangesAsync(cancellationToken);

                Console.WriteLine("✓ Database Seeding Completed Successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Seeding Error occurred:");
                Console.WriteLine($"   {ex.Message}");
                // យើងមិន throw ទេ ដើម្បីកុំឱ្យវាគាំងបិទ App ទាំងមូលពេលរត់ Fail
            }
        }

        // 🛑 មុខងារនេះរត់ពេលបិទ Server (យើងមិនបាច់ធ្វើអ្វីទេ ទុកវាឱ្យត្រឡប់ត្រឹម Task.CompletedTask ចុះ)
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        // ----------------------------------------------------------------------------------
        // 🔒 មុខងារលម្អិតសម្រាប់ពិនិត្យ និងបង្កើតទិន្នន័យ (Logic ចាស់របស់ប្រូ រក្សាទុកដដែល)
        // ----------------------------------------------------------------------------------

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
                Console.WriteLine("✓ User Default role created.");
            }
            else
            {
                Console.WriteLine("✓ User Default role already exists.");
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
                    StaffId = null,
                    CustomerId = null,
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
                Console.WriteLine("✓ SuperAdmin user created (StaffId=null, CustomerId=null).");
            }
            else
            {
                Console.WriteLine("✓ SuperAdmin user already exists.");
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

                Console.WriteLine($"✓ {permissionsToAdd.Count} permissions added to SuperAdmin role.");
            }
            else
            {
                Console.WriteLine("✓ SuperAdmin role already has all permissions.");
            }
        }
    }
}