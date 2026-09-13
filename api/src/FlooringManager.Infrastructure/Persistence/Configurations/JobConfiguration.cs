using FlooringManager.Domain.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlooringManager.Infrastructure.Persistence.Configurations;

public sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> b)
    {
        b.ToTable("jobs");
        b.HasKey(x => x.Id);

        b.Property(x => x.CompanyId).IsRequired();
        b.Property(x => x.CustomerId).IsRequired();
        b.Property(x => x.PropertyId).IsRequired();
        b.Property(x => x.EstimateId).IsRequired();

        b.Property(x => x.JobNumber).HasMaxLength(50).IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();

        b.Property(x => x.Description).HasMaxLength(4000);
        b.Property(x => x.InternalNotes).HasMaxLength(4000);
        b.Property(x => x.CustomerNotes).HasMaxLength(4000);
        b.Property(x => x.CreatedAt).IsRequired();

        b.HasIndex(x => x.CompanyId);
        b.HasIndex(x => new { x.CompanyId, x.JobNumber }).IsUnique();
        b.HasIndex(x => new { x.CompanyId, x.Status });
        b.HasIndex(x => x.EstimateId).IsUnique();

        b.HasMany(x => x.Rooms)
            .WithOne(r => r.Job)
            .HasForeignKey(r => r.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
