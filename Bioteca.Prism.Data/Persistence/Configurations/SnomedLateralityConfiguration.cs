using Bioteca.Prism.Domain.Entities.Snomed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for SnomedLaterality entity
/// </summary>
public class SnomedLateralityConfiguration : IEntityTypeConfiguration<SnomedLaterality>
{
    public void Configure(EntityTypeBuilder<SnomedLaterality> builder)
    {
        builder.ToTable("snomed_laterality");

        // Primary key
        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        // Basic properties
        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("ix_snomed_laterality_is_active");
    }
}
