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
        public DbSet<Book> Books { get; set; } = null!;
        public DbSet<BookIssue> BookIssues { get; set; } = null!;
        public DbSet<LibraryMember> LibraryMembers { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
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

            builder.Entity<LibraryMember>(entity =>
{
    entity.ToTable("library_members");
    entity.HasKey(x => x.Id);

    entity.Property(x => x.MembershipNo).HasMaxLength(50).IsRequired();
    entity.Property(x => x.MembershipType).HasMaxLength(50).IsRequired();
    entity.Property(x => x.Email).HasMaxLength(100).IsRequired();
    entity.Property(x => x.Status).HasDefaultValue(0); // Pending by default
    entity.Property(x => x.IsActive).HasDefaultValue(true);
    entity.Property(x => x.MaxBooksAllowed).HasDefaultValue(5);
    entity.Property(x => x.Address).HasMaxLength(500);
    entity.Property(x => x.PhoneNumber).HasMaxLength(20);
    entity.Property(x => x.IsDeleted).HasDefaultValue(false);
    entity.Property(x => x.ApproveBy).IsRequired(false); // Nullable - null until approved/rejected/cancelled

    // Indexes
    entity.HasIndex(x => x.MembershipNo).IsUnique();
    entity.HasIndex(x => x.Email); // Non-unique index for search

    // Relationship: LibraryMember -> Person (the member)
    entity.HasOne(x => x.Person)
          .WithMany()
          .HasForeignKey(x => x.PersonId)
          .OnDelete(DeleteBehavior.Restrict);

    // Relationship: LibraryMember -> Person (who approved/rejected/cancelled)
    entity.HasOne(x => x.ApprovedByUser)
          .WithMany()
          .HasForeignKey(x => x.ApproveBy)
          .OnDelete(DeleteBehavior.Restrict)
          .IsRequired(false);
});

            builder.Entity<BookIssue>(entity =>
            {
                entity.ToTable("book_issues");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Issued");
                entity.Property(x => x.Notes).HasMaxLength(500);
                entity.Property(x => x.IsDeleted).HasDefaultValue(false);

                entity.HasOne(x => x.Book)
                    .WithMany()
                    .HasForeignKey(x => x.BookId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.LibraryMember)
                    .WithMany()
                    .HasForeignKey(x => x.LibraryMemberId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.IssuedByPerson)
                    .WithMany()
                    .HasForeignKey(x => x.IssuedByPersonId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ---------------- BOOK ----------------
            builder.Entity<Book>(entity =>
            {
                entity.ToTable("books");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Author).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Subject).HasMaxLength(100).IsRequired();
                entity.Property(x => x.ISBN).HasMaxLength(50);
                entity.Property(x => x.Publisher).HasMaxLength(100);
                entity.Property(x => x.Edition).HasMaxLength(50);
                entity.Property(x => x.Price).HasPrecision(18, 2);
                entity.Property(x => x.RackNo).HasMaxLength(50).IsRequired();
                entity.Property(x => x.No).HasMaxLength(50).IsRequired();
                entity.Property(x => x.IsDeleted).HasDefaultValue(false);

                // Category relationship
                entity.HasOne(x => x.Category)
                      .WithMany(c => c.Books)
                      .HasForeignKey(x => x.CategoryId)
                      .OnDelete(DeleteBehavior.SetNull);
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
            });
        }
    }
}
