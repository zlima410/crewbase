using FlooringManager.Domain.Estimates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlooringManager.Infrastructure.Persistence.Configurations;

public sealed class EstimateConfiguration : IEntityTypeConfiguration<Estimate>
{
    public void Configure(EntityTypeBuilder<Estimate> b)
    {
        b.ToTable("estimates");
        b.HasKey(x => x.Id);

        b.Property(x => x.CompanyId).IsRequired();
        b.Property(x => x.CustomerId).IsRequired();
        b.Property(x => x.PropertyId).IsRequired();

        b.Property(x => x.EstimateNumber).HasMaxLength(50).IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();

        b.Property(x => x.LaborSubtotal).HasPrecision(18, 4);
        b.Property(x => x.MaterialSubtotal).HasPrecision(18, 4);
        b.Property(x => x.TaxRate).HasPrecision(9, 4);
        b.Property(x => x.Tax).HasPrecision(18, 4);
        b.Property(x => x.Total).HasPrecision(18, 4);

        b.Property(x => x.Notes).HasMaxLength(4000);
        b.Property(x => x.CreatedDate).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();

        b.HasIndex(x => x.CompanyId);
        b.HasIndex(x => new { x.CompanyId, x.EstimateNumber }).IsUnique();
        b.HasIndex(x => new { x.CompanyId, x.Status });

        b.HasMany(x => x.Rooms)
            .WithOne(r => r.Estimate)
            .HasForeignKey(r => r.EstimateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}