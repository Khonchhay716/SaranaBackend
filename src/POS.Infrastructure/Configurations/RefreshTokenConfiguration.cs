using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Domain.Entities;

namespace POS.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");

            builder.HasKey(rt => rt.Id);

            builder.Property(rt => rt.Token)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(rt => rt.ExpiryDate)
                .IsRequired();

            builder.Property(rt => rt.IsRevoked)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(rt => rt.CreatedDate)
                .IsRequired();

            builder.HasOne(rt => rt.Person)
                .WithMany()
                .HasForeignKey(rt => rt.PersonId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(rt => rt.Token).IsUnique();
            builder.HasIndex(rt => rt.PersonId);
            builder.HasIndex(rt => rt.ExpiryDate);
        }
    }
}