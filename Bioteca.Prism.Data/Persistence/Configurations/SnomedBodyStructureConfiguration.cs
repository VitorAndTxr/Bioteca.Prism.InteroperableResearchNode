using Bioteca.Prism.Domain.Entities.Snomed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for SnomedBodyStructure entity
/// </summary>
public class SnomedBodyStructureConfiguration : IEntityTypeConfiguration<SnomedBodyStructure>
{
    public void Configure(EntityTypeBuilder<SnomedBodyStructure> builder)
    {
        builder.ToTable("snomed_body_structure");

        // Primary key
        builder.HasKey(x => x.SnomedCode);
        builder.Property(x => x.SnomedCode)
            .HasColumnName("snomed_code")
            .HasMaxLength(50)
            .IsRequired();

        // Foreign keys
        builder.Property(x => x.BodyRegionCode)
            .HasColumnName("body_region_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ParentStructureCode)
            .HasColumnName("parent_structure_code")
            .HasMaxLength(50);

        // Basic properties
        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.StructureType)
            .HasColumnName("structure_type")
            .HasMaxLength(100)
            .IsRequired();

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

        // Relationships
        builder.HasOne(x => x.BodyRegion)
            .WithMany(x => x.BodyStructures)
            .HasForeignKey(x => x.BodyRegionCode)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing hierarchy
        builder.HasOne(x => x.ParentStructure)
            .WithMany(x => x.SubStructures)
            .HasForeignKey(x => x.ParentStructureCode)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.BodyRegionCode)
            .HasDatabaseName("ix_snomed_body_structure_body_region_code");

        builder.HasIndex(x => x.ParentStructureCode)
            .HasDatabaseName("ix_snomed_body_structure_parent_structure_code");

        builder.HasIndex(x => x.StructureType)
            .HasDatabaseName("ix_snomed_body_structure_structure_type");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("ix_snomed_body_structure_is_active");
    }
}
