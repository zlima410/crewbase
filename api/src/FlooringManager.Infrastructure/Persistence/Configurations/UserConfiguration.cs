using FlooringManager.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlooringManager.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.AuthProviderUserId).IsRequired();
        builder.HasIndex(u => u.AuthProviderUserId).IsUnique();

        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(320).IsRequired();
        builder.Property(u => u.Role).HasConversion<int>().IsRequired();
        builder.Property(u => u.IsActive).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();

        builder.HasOne(u => u.Company)
            .WithMany(c => c.Users)
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.CompanyId);
    }
}