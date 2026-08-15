using FlooringManager.Domain.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlooringManager.Infrastructure.Persistence.Configurations;

public sealed class JobRoomConfiguration : IEntityTypeConfiguration<JobRoom>
{
    public void Configure(EntityTypeBuilder<JobRoom> b)
    {
        b.ToTable("job_rooms");
        b.HasKey(x => x.Id);

        b.Property(x => x.JobId).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.FlooringType).HasConversion<int>().IsRequired();
        b.Property(x => x.WorkType).HasConversion<int>().IsRequired();
        b.Property(x => x.InstallationMethod).HasConversion<int?>();
        b.Property(x => x.FinishType).HasConversion<int?>();
        b.Property(x => x.Notes).HasMaxLength(2000);

        b.Property(x => x.SquareFeet).HasPrecision(18, 4);
        b.Property(x => x.BillableSquareFeet).HasPrecision(18, 4);

        b.HasIndex(x => new { x.JobId, x.Position });
    }
}
