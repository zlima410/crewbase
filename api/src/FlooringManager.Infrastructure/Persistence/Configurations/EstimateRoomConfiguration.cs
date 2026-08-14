using FlooringManager.Domain.Estimates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlooringManager.Infrastructure.Persistence.Configurations;

public sealed class EstimateRoomConfiguration : IEntityTypeConfiguration<EstimateRoom>
{
    public void Configure(EntityTypeBuilder<EstimateRoom> b)
    {
        b.ToTable("estimate_rooms");
        b.HasKey(x => x.Id);

        b.Property(x => x.EstimateId).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.FlooringType).HasConversion<int>().IsRequired();
        b.Property(x => x.WorkType).HasConversion<int>().IsRequired();

        b.Property(x => x.LengthFeet).HasPrecision(18, 4);
        b.Property(x => x.WidthFeet).HasPrecision(18, 4);
        b.Property(x => x.WastePercentage).HasPrecision(9, 4);
        b.Property(x => x.SquareFeet).HasPrecision(18, 4);
        b.Property(x => x.BillableSquareFeet).HasPrecision(18, 4);
        b.Property(x => x.LaborRatePerSqFt).HasPrecision(18, 4);
        b.Property(x => x.MaterialRatePerSqFt).HasPrecision(18, 4);

        b.HasIndex(x => new { x.EstimateId, x.Position });
    }
}