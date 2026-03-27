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
        public DbSet<Discount> Discounts => Set<Discount>();
        public DbSet<ProductDiscount> ProductDiscounts => Set<ProductDiscount>();

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
            builder.Entity<Person>(entity =>
            {
                entity.ToTable("persons");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.FirstName).HasMaxLength(50).IsRequired();
                entity.Property(x => x.LastName).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Username).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Email).HasMaxLength(100).IsRequired();
                entity.Property(x => x.IsActive).HasDefaultValue(true);
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

                // Unique constraint: SerialNo must be unique per product
                entity.HasIndex(x => new { x.ProductId, x.SerialNo })
                      .IsUnique()
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

                entity.Property(x => x.SubTotal).HasPrecision(18, 2);
                entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
                entity.Property(x => x.TaxAmount).HasPrecision(18, 2);
                entity.Property(x => x.TotalAmount).HasPrecision(18, 2);

                entity.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(x => x.PaymentStatus)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(x => x.SaleType)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.HasMany(x => x.OrderItems)
                    .WithOne(oi => oi.Order)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
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
        }
    }
}