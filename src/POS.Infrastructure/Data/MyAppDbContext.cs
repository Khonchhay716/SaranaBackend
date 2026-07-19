using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;
using POS.Domain.Entities.StockManagement;
using POS.Domain.Common;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;

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
        public DbSet<Person> Persons { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<PersonRole> PersonRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<ProductDiscount> ProductDiscounts { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<LeaveBalance> LeaveBalances { get; set; }
        public DbSet<PointSetup> PointSetups { get; set; }

        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<SerialStock> SerialStocks { get; set; }
        public DbSet<NonSerialStock> NonSerialStocks { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<StockAdjustment> StockAdjustments { get; set; }
        public DbSet<StockReturn> StockReturns { get; set; }
        public DbSet<StockReturnItem> StockReturnItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

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

            foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedDate = utcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedDate = utcNow;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedDate = utcNow;
                }
            }
        }

        // ----------------- Model Building -----------------
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.HasDefaultSchema("pos");

            // ---------------- PERSON ----------------
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

            builder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(500);
                entity.HasIndex(x => x.Name).IsUnique();
            });

            builder.Entity<Permission>(entity =>
            {
                entity.ToTable("permissions");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(500);
                entity.HasIndex(x => x.Name).IsUnique();
            });

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

            /// stock management 
            builder.Entity<Supplier>(entity =>
            {
                entity.ToTable("suppliers");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Phone).HasMaxLength(20);
                entity.Property(x => x.Email).HasMaxLength(200);
                entity.Property(x => x.Address).HasMaxLength(500);
            });

            builder.Entity<Product>(entity =>
            {
                entity.ToTable("products");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Code).HasMaxLength(50);
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(500);
                entity.Property(x => x.ImageUrl).HasMaxLength(500);
                entity.Property(x => x.ProductType).HasConversion<string>();
                entity.Property(x => x.Unit).HasMaxLength(20);
                entity.Property(x => x.CostPrice).HasColumnType("decimal(18,2)");
                entity.Property(x => x.SalePrice).HasColumnType("decimal(18,2)");
                entity.Property(x => x.LowStockThreshold).HasDefaultValue(0);
                entity.Property(x => x.StockQuantity).HasDefaultValue(0);
                entity.HasIndex(x => x.Code)
                        .IsUnique()
                        .HasFilter("\"Code\" IS NOT NULL");
                entity.HasOne(x => x.Category)
                        .WithMany(c => c.Products)
                        .HasForeignKey(x => x.CategoryId)
                        .IsRequired(false)
                        .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<SerialStock>(entity =>
            {
                entity.ToTable("serial_stocks");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.SerialNo).IsUnique();
                entity.Property(x => x.SerialNo).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Status).HasConversion<string>();

                entity.HasOne(x => x.Product)
                      .WithMany(x => x.SerialStocks)
                      .HasForeignKey(x => x.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);

            });

            builder.Entity<NonSerialStock>(entity =>
            {
                entity.ToTable("non_serial_stocks");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Quantity).HasDefaultValue(0);
                entity.HasOne(x => x.Product)
                      .WithMany(x => x.NonSerialStocks)
                      .HasForeignKey(x => x.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<StockMovement>(entity =>
            {
                entity.ToTable("stock_movements");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Type).HasConversion<string>();
                entity.Property(x => x.Reference).HasMaxLength(100);

                entity.HasOne(x => x.Product)
                      .WithMany(x => x.StockMovements)
                      .HasForeignKey(x => x.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Supplier)
                      .WithMany(x => x.StockMovements)
                      .HasForeignKey(x => x.SupplierId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(x => x.OrderItem)
                      .WithMany()
                      .HasForeignKey(x => x.OrderItemId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
            });


            builder.Entity<StockAdjustment>(entity =>
            {
                entity.ToTable("stock_adjustments");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Reason).HasConversion<string>();

                entity.HasOne(x => x.Product)
                      .WithMany(x => x.StockAdjustments)
                      .HasForeignKey(x => x.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<StockReturn>(entity =>
       {
           entity.ToTable("stock_returns");
           entity.HasKey(x => x.Id);

           entity.HasIndex(x => x.ReturnNo).IsUnique();
           entity.Property(x => x.ReturnNo).HasMaxLength(50).IsRequired();

           entity.Property(x => x.Status)
                 .HasConversion<string>()
                 .HasMaxLength(20)
                 .HasDefaultValue(ReturnStatus.Completed);

           entity.Property(x => x.TotalAmount)
                 .HasColumnType("decimal(18,2)")
                 .HasDefaultValue(0m);

           entity.HasOne(x => x.Supplier)
                 .WithMany()
                 .HasForeignKey(x => x.SupplierId)
                 .OnDelete(DeleteBehavior.Restrict);
       });

            builder.Entity<StockReturnItem>(entity =>
            {
                entity.ToTable("stock_return_items");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Reason).HasConversion<string>();

                entity.Property(x => x.UnitPrice)
                      .HasColumnType("decimal(18,2)")
                      .HasDefaultValue(0m);

                entity.Property(x => x.TotalPrice)
                      .HasColumnType("decimal(18,2)")
                      .HasDefaultValue(0m);

                entity.Property(x => x.SerialNumbers).HasColumnType("jsonb");

                entity.HasOne(x => x.StockReturn)
                      .WithMany(x => x.Items)
                      .HasForeignKey(x => x.StockReturnId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Product)
                      .WithMany()
                      .HasForeignKey(x => x.ProductId)
                      .OnDelete(DeleteBehavior.Restrict); 
            });


            builder.Entity<Discount>(entity =>
            {
                entity.ToTable("discounts");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(500);
                entity.Property(x => x.Type).HasMaxLength(20).IsRequired();
                entity.Property(x => x.Value).HasColumnType("decimal(18,2)");
                entity.Property(x => x.MinOrderAmount).HasColumnType("decimal(18,2)");
                entity.Property(x => x.IsActive).HasDefaultValue(true);
                entity.Property(x => x.IsAllProducts).HasDefaultValue(false);
            });

            builder.Entity<ProductDiscount>(entity =>
            {
                entity.ToTable("product_discounts");
                entity.HasKey(x => x.Id);

                entity.HasOne(x => x.Product)
                      .WithMany(p => p.ProductDiscounts)
                      .HasForeignKey(x => x.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Discount)
                      .WithMany(d => d.ProductDiscounts)
                      .HasForeignKey(x => x.DiscountId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Add this block inside MyAppDbContext.OnModelCreating, alongside the other
            // entity configurations (e.g. right after the ProductDiscount block).

            // OnModelCreating
            builder.Entity<Order>(entity =>
            {
                entity.ToTable("orders");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.OrderNo).IsUnique();
                entity.Property(x => x.OrderNo).HasMaxLength(50).IsRequired();
                entity.Property(x => x.SubTotal).HasColumnType("decimal(18,2)");
                entity.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(x => x.PointEarned).HasColumnType("decimal(18,4)");
                entity.Property(x => x.PointUsed).HasColumnType("decimal(18,4)");
                entity.Property(x => x.Status).HasConversion<string>();
                entity.Property(x => x.PaymentMethod).HasConversion<string>();
                entity.Property(x => x.Note).HasMaxLength(500);

                entity.HasOne(x => x.Customer)
                      .WithMany()
                      .HasForeignKey(x => x.CustomerId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("order_items");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
                entity.Property(x => x.SerialNumbers).HasColumnType("jsonb");

                entity.HasOne(x => x.Order)
                      .WithMany(o => o.Items)
                      .HasForeignKey(x => x.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Product)
                      .WithMany()
                      .HasForeignKey(x => x.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Discount)
                      .WithMany()
                      .HasForeignKey(x => x.DiscountId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}