using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Domain.Common;
using POS.Application.Common.Interfaces;

namespace POS.Infrastructure.Data
{
    public class MyAppDbContext : DbContext, IMyAppDbContext
    {
        public MyAppDbContext(DbContextOptions<MyAppDbContext> options) : base(options)
        {
            // PostgreSQL timestamp switches
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
        }

        // ----------------- DbSets -----------------
        public DbSet<Person> Persons { get; set; } = null!;
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Permission> Permissions { get; set; } = null!;
        public DbSet<PersonRole> PersonRoles { get; set; } = null!;
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Branch> Branches { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<SerialNumber> SerialNumbers { get; set; } = null!;
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Discount> Discounts => Set<Discount>();
        public DbSet<ProductDiscount> ProductDiscounts => Set<ProductDiscount>();
        public DbSet<LeaveType> LeaveTypes { get; set; } = null!;
        public DbSet<LeaveRequest> LeaveRequests { get; set; } = null!;
        public DbSet<LeaveBalance> LeaveBalances { get; set; } = null!;
        public DbSet<PointSetup> PointSetups { get; set; } = null!;

        // ----------------- Configuration -----------------
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(
                    "Host=localhost;Port=5432;Database=mytest;Username=postgres;Password=chhay33333333;");
            }
        }

        // ----------------- SaveChanges -----------------
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        private void UpdateTimestamps()
        {
            var utcNow = DateTimeOffset.UtcNow;

            // Handle entities that implement IHasTimestamps (if you still have any)
            foreach (var entry in ChangeTracker.Entries<IHasTimestamps>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedDate = utcNow;
                    entry.Entity.UpdatedDate = utcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedDate = utcNow;
                }
            }

            // ADDED: Handle entities that inherit from BaseAuditableEntity
            foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedDate = utcNow;
                    // You can set CreatedBy here if you have access to current user context
                    // entry.Entity.CreatedBy = _currentUserService.UserId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedDate = utcNow;
                    // You can set UpdatedBy here if you have access to current user context
                    // entry.Entity.UpdatedBy = _currentUserService.UserId;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    // Implement soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedDate = utcNow;
                    // You can set DeletedBy here if you have access to current user context
                    // entry.Entity.DeletedBy = _currentUserService.UserId;
                }
            }
        }

        // ----------------- Model Building -----------------
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.HasDefaultSchema("pos");

            // ---------------- PERSON ----------------
            // builder.Entity<Person>(entity =>
            // {
            //     entity.ToTable("persons");
            //     entity.HasKey(x => x.Id);
            //     entity.Property(x => x.FirstName).HasMaxLength(50).IsRequired();
            //     entity.Property(x => x.LastName).HasMaxLength(50).IsRequired();
            //     entity.Property(x => x.Username).HasMaxLength(50).IsRequired();
            //     entity.Property(x => x.Email).HasMaxLength(100).IsRequired();
            //     entity.Property(x => x.IsActive).HasDefaultValue(true);
            // });


            builder.Entity<Person>(entity =>
           {
               entity.ToTable("persons");
               entity.HasKey(x => x.Id);

               // ព័ត៌មាន Login
               entity.Property(x => x.Username).HasMaxLength(50).IsRequired();
               entity.Property(x => x.Email).HasMaxLength(100).IsRequired();
               entity.Property(x => x.PasswordHash).IsRequired();
               entity.Property(x => x.IsActive).HasDefaultValue(true);

               // កំណត់ប្រភេទ User (Staff ឬ Customer)
               entity.Property(x => x.Type)
                     .HasConversion<string>()
                     .IsRequired();

               //determind relationship One-to-One with Staff
               entity.HasOne(p => p.Staff)
                     .WithOne(s => s.Person)
                     .HasForeignKey<Person>(p => p.StaffId)
                     .OnDelete(DeleteBehavior.SetNull);

               // determind One-to-One with Customer
               entity.HasOne(p => p.Customer)
                     .WithOne(c => c.Person)
                     .HasForeignKey<Person>(p => p.CustomerId)
                     .OnDelete(DeleteBehavior.SetNull);
           });

            builder.Entity<Staff>(entity =>
            {
                entity.ToTable("staffs");
                entity.Property(x => x.FirstName).HasMaxLength(50).IsRequired();
                entity.Property(x => x.LastName).HasMaxLength(50).IsRequired();
                entity.Property(x => x.PhoneNumber).HasMaxLength(20);
                entity.Property(x => x.ImageProfile).HasMaxLength(500);
            });

            builder.Entity<Customer>(entity =>
            {
                entity.ToTable("customers");
                entity.Property(x => x.FirstName).HasMaxLength(50).IsRequired();
                entity.Property(x => x.LastName).HasMaxLength(50).IsRequired();
                entity.Property(x => x.TotalPoint).HasDefaultValue(0);
                entity.Property(x => x.ImageProfile).HasMaxLength(500);
            });

            // ---------------- ROLE ----------------
            builder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(500);
                entity.HasIndex(x => x.Name).IsUnique();
            });

            // ---------------- PERMISSION ----------------
            builder.Entity<Permission>(entity =>
            {
                entity.ToTable("permissions");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(500);
                entity.HasIndex(x => x.Name).IsUnique();
            });

            // ---------------- ROLE_PERMISSION ----------------
            builder.Entity<RolePermission>(entity =>
            {
                entity.ToTable("role_permissions");
                entity.HasKey(x => new { x.RoleId, x.PermissionName });
                entity.Property(x => x.PermissionName)
                      .HasColumnName("permission_name")
                      .HasMaxLength(150)
                      .IsRequired();
                entity.Property(x => x.RoleId).HasColumnName("role_id");
                entity.HasOne(x => x.Role)
                      .WithMany(r => r.RolePermissions)
                      .HasForeignKey(x => x.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------------- PERSON_ROLE ----------------
            builder.Entity<PersonRole>(entity =>
            {
                entity.ToTable("person_roles");
                entity.HasKey(x => new { x.PersonId, x.RoleId });
                entity.HasOne(x => x.Person)
                      .WithMany(p => p.PersonRoles)
                      .HasForeignKey(x => x.PersonId);
                entity.HasOne(x => x.Role)
                      .WithMany(r => r.PersonRoles)
                      .HasForeignKey(x => x.RoleId);
            });

            // ---------------- REFRESH TOKEN ----------------
            builder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("refresh_tokens");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Token).HasMaxLength(500).IsRequired();
                entity.HasOne(rt => rt.Person)
                      .WithMany(p => p.RefreshTokens)
                      .HasForeignKey(rt => rt.PersonId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------------- CATEGORY ----------------
            builder.Entity<Category>(entity =>
            {
                entity.ToTable("categories");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(500);
                entity.Property(x => x.IsActive).HasDefaultValue(true);
                entity.Property(x => x.IsDeleted).HasDefaultValue(false);
                entity.HasIndex(x => x.Name).IsUnique();
                entity.Property(x => x.Image)
                      .HasColumnType("text");
            });

            builder.Entity<Branch>(entity =>
            {
                entity.ToTable("branches");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.BranchName)
                    .HasMaxLength(200)
                    .IsRequired();
                entity.Property(x => x.Logo)
                    .HasColumnType("text");
                entity.Property(x => x.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("Active")
                    .IsRequired();
                entity.Property(x => x.Description)
                    .HasMaxLength(1000);
                entity.Property(x => x.IsDeleted)
                    .HasDefaultValue(false);
                entity.HasIndex(x => x.BranchName)
                    .IsUnique()
                    .HasFilter("\"IsDeleted\" = false");
                entity.HasMany(x => x.Products)
                    .WithOne(p => p.Branch)
                    .HasForeignKey(p => p.BranchId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ---------------- PRODUCT ----------------
            builder.Entity<Product>(entity =>
            {
                entity.ToTable("products");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(1000);
                entity.Property(x => x.SKU).HasMaxLength(50);
                entity.Property(x => x.Barcode).HasMaxLength(50);
                entity.Property(x => x.Price).HasPrecision(18, 2);
                entity.Property(x => x.CostPrice).HasPrecision(18, 2);
                entity.Property(x => x.IsDeleted).HasDefaultValue(false);

                // Category relationship
                entity.HasOne(x => x.Category)
                      .WithMany(c => c.Products)
                      .HasForeignKey(x => x.CategoryId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ---------------- SERIAL NUMBER ----------------
            builder.Entity<SerialNumber>(entity =>
            {
                entity.ToTable("serial_numbers");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.SerialNo).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Status).HasMaxLength(50);
                entity.Property(x => x.Notes).HasMaxLength(500);
                entity.Property(x => x.IsDeleted).HasDefaultValue(false);

                // Relationship with Product
                entity.HasOne(x => x.Product)
                      .WithMany(p => p.SerialNumbers)
                      .HasForeignKey(x => x.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(x => x.SerialNo)
                      .IsUnique()
                      .HasDatabaseName("UX_SerialNumber_SerialNo")
                      .HasFilter("\"IsDeleted\" = false");
            });

            builder.Entity<StockMovement>(entity =>
            {
                entity.ToTable("stock_movements");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Type).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Quantity).IsRequired();
                entity.Property(x => x.Price).HasPrecision(18, 2);
                entity.Property(x => x.CostPrice).HasPrecision(18, 2);
                entity.Property(x => x.Notes).HasMaxLength(500);
                entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            });

            builder.Entity<Order>(entity =>
            {
                entity.ToTable("orders");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.OrderNumber)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasIndex(x => x.OrderNumber)
                    .IsUnique();
                entity.Property(x => x.EarnedPoints).HasDefaultValue(0);
                entity.Property(x => x.SubTotal).HasPrecision(18, 2);
                entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
                entity.Property(x => x.TaxAmount).HasPrecision(18, 2);
                entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
                entity.Property(x => x.CashReceived)
                    .HasPrecision(18, 2)
                    .HasDefaultValue(0);
                entity.Property(x => x.PointsUsed)
                      .HasDefaultValue(0);

                entity.Property(x => x.Status)
                    .HasConversion<int>();

                entity.Property(x => x.PaymentStatus)
                    .HasConversion<int>();

                entity.Property(x => x.SaleType)
                    .HasConversion<int>();

                entity.HasMany(x => x.OrderItems)
                    .WithOne(oi => oi.Order)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.Staff)
                    .WithMany()
                    .HasForeignKey(x => x.StaffId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(x => x.OrderDate);
                entity.HasIndex(x => x.StaffId);
                entity.HasIndex(x => x.CustomerId);
                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.PaymentStatus);
            });

            builder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("order_items");
                entity.HasKey(x => x.Id);
                entity.HasOne(oi => oi.SerialNumber)
                    .WithOne(sn => sn.OrderItem)
                    .HasForeignKey<OrderItem>(oi => oi.SerialNumberId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Discount>(entity =>
            {
                entity.ToTable("discounts");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(1000);
                entity.Property(x => x.Type)
                    .HasMaxLength(20)
                    .HasDefaultValue("Percentage")
                    .IsRequired();
                entity.Property(x => x.Value).HasPrecision(18, 2);
                entity.Property(x => x.MinOrderAmount).HasPrecision(18, 2);
                entity.Property(x => x.IsActive).HasDefaultValue(true);
                entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            });

            builder.Entity<ProductDiscount>(entity =>
            {
                entity.ToTable("product_discounts");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.IsDeleted).HasDefaultValue(false);
                entity.HasIndex(x => new { x.DiscountId, x.ProductId })
                      .IsUnique()
                      .HasFilter("\"IsDeleted\" = false");
                entity.HasOne(x => x.Discount)
                      .WithMany(d => d.ProductDiscounts)
                      .HasForeignKey(x => x.DiscountId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.Product)
                      .WithMany()
                      .HasForeignKey(x => x.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------------- LEAVE TYPE ----------------
            builder.Entity<LeaveType>(entity =>
            {
                entity.ToTable("leave_types");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name)
                      .HasMaxLength(100)
                      .IsRequired();
                entity.Property(x => x.Description)
                      .HasMaxLength(500);
                entity.Property(x => x.MaxDaysPerYear)
                      .IsRequired();
                entity.Property(x => x.IsActive)
                      .HasDefaultValue(true);
                entity.Property(x => x.IsDeleted)
                      .HasDefaultValue(false);
                entity.HasIndex(x => x.Name)
                      .IsUnique()
                      .HasFilter("\"IsDeleted\" = false");
            });

            // ---------------- LEAVE REQUEST ----------------
            builder.Entity<LeaveRequest>(entity =>
            {
                entity.ToTable("leave_requests");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Reason)
                      .HasMaxLength(500)
                      .IsRequired();
                entity.Property(x => x.Status)
                      .HasMaxLength(20)
                      .HasDefaultValue("Pending")
                      .IsRequired();
                entity.Property(x => x.ApprovalNote)
                      .HasMaxLength(500);
                entity.Property(x => x.IsDeleted)
                      .HasDefaultValue(false);
                entity.Property(x => x.TotalDays).HasPrecision(5, 1);
                entity.Property(x => x.Session).HasMaxLength(20).HasDefaultValue("FullDay");

                // Staff who requested
                entity.HasOne(x => x.Staff)
                      .WithMany()
                      .HasForeignKey(x => x.StaffId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Leave type
                entity.HasOne(x => x.LeaveType)
                      .WithMany(lt => lt.LeaveRequests)
                      .HasForeignKey(x => x.LeaveTypeId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Approver (supervisor)
                entity.HasOne(x => x.Approver)
                      .WithMany()
                      .HasForeignKey(x => x.ApproverId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);

                entity.HasIndex(x => x.StaffId);
                entity.HasIndex(x => x.ApproverId);
                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.StartDate);
            });

            // ---------------- LEAVE BALANCE ----------------
            builder.Entity<LeaveBalance>(entity =>
            {
                entity.ToTable("leave_balances");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Year).IsRequired();
                entity.Property(x => x.TotalDays).HasPrecision(5, 1);
                entity.Property(x => x.UsedDays).HasPrecision(5, 1).HasDefaultValue(0);
                entity.Property(x => x.IsDeleted).HasDefaultValue(false);

                // Ignore computed property
                entity.Ignore(x => x.RemainingDays);

                entity.HasOne(x => x.Staff)
                      .WithMany()
                      .HasForeignKey(x => x.StaffId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.LeaveType)
                      .WithMany()
                      .HasForeignKey(x => x.LeaveTypeId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Unique: one balance per staff per leave type per year
                entity.HasIndex(x => new { x.StaffId, x.LeaveTypeId, x.Year })
                      .IsUnique()
                      .HasFilter("\"IsDeleted\" = false");
            });
            builder.Entity<PointSetup>(entity =>
            {
                entity.ToTable("point_setups");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.PointValue)
                    .HasPrecision(10, 4)
                    .HasDefaultValue(0);
                entity.Property(x => x.MinOrderAmount)
                    .HasPrecision(10, 2)
                    .HasDefaultValue(0);
                entity.Property(x => x.MaxPointPerOrder)
                    .IsRequired(false);
                entity.Property(x => x.PointsPerRedemption)
                    .HasPrecision(10, 4)
                    .HasDefaultValue(0);
                entity.Property(x => x.IsActive)
                    .HasDefaultValue(false);

                entity.HasData(new PointSetup
                {
                    Id = 1,
                    PointValue = 0,
                    MinOrderAmount = 0,
                    MaxPointPerOrder = null,
                    PointsPerRedemption = 0,
                    IsActive = false,
                    CreatedDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                });
            });
        }
    }
}