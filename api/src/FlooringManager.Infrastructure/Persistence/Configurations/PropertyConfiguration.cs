using FlooringManager.Domain.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlooringManager.Infrastructure.Persistence.Configurations;

public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("properties");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.CustomerId).IsRequired();
        builder.HasIndex(p => p.CustomerId);
        builder.Property(p => p.StreetAddress).HasMaxLength(300).IsRequired();
        builder.Property(p => p.City).HasMaxLength(100).IsRequired();
        builder.Property(p => p.State).HasMaxLength(50).IsRequired();
        builder.Property(p => p.PostalCode).HasMaxLength(20).IsRequired();
        builder.Property(p => p.AccessNotes).HasMaxLength(2000);
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
        
        builder.HasOne(p => p.Customer)
            .WithMany(c => c.Properties)
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}