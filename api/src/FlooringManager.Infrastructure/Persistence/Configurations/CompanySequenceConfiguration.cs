using FlooringManager.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlooringManager.Infrastructure.Persistence.Configurations;

public sealed class CompanySequenceConfiguration : IEntityTypeConfiguration<CompanySequence>
{
    public void Configure(EntityTypeBuilder<CompanySequence> builder)
    {
        builder.ToTable("company_sequences");

        builder.HasKey(s => new { s.CompanyId, s.Prefix });

        builder.Property(s => s.Prefix).HasMaxLength(10).IsRequired();
        builder.Property(s => s.LastValue).IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
