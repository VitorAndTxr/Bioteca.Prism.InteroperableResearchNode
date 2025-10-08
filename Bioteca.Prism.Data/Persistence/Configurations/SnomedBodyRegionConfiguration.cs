using Bioteca.Prism.Domain.Entities.Snomed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for SnomedBodyRegion entity
/// </summary>
public class SnomedBodyRegionConfiguration : IEntityTypeConfiguration<SnomedBodyRegion>
{
    public void Configure(EntityTypeBuilder<SnomedBodyRegion> builder)
    {
        builder.ToTable("snomed_body_region");

        // Primary key
        builder.HasKey(x => x.SnomedCode);
        builder.Property(x => x.SnomedCode)
            .HasColumnName("snomed_code")
            .HasMaxLength(50)
            .IsRequired();

        // Basic properties
        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ParentRegionCode)
            .HasColumnName("parent_region_code")
            .HasMaxLength(50);

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        // Timestamps
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Relationships - self-referencing hierarchy
        builder.HasOne(x => x.ParentRegion)
            .WithMany(x => x.SubRegions)
            .HasForeignKey(x => x.ParentRegionCode)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.ParentRegionCode)
            .HasDatabaseName("ix_snomed_body_region_parent_region_code");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("ix_snomed_body_region_is_active");
    }
}
