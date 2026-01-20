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
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Person> Persons { get; set; } = null!;
        public DbSet<Coupon> Coupons { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Permission> Permissions { get; set; } = null!;
        public DbSet<PersonRole> PersonRoles { get; set; } = null!;
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

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
        }

        // ----------------- Model Building -----------------
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.HasDefaultSchema("pos");

            // ---------------- PRODUCT ----------------
            builder.Entity<Product>(entity =>
            {
                entity.ToTable("products");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Price).HasPrecision(18, 2);
                entity.Property(x => x.IsActive).HasDefaultValue(true);
                entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            });

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
        }
    }
}
